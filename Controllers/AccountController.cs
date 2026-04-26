using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.Services;
using ExamSystem.ViewModels;
using BC = BCrypt.Net.BCrypt;

namespace ExamSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly ExamDbContext _db;
        private readonly IEmailSender _emailSender;

        public AccountController(ExamDbContext db, IEmailSender emailSender)
        {
            _db = db;
            _emailSender = emailSender;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToHome();

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users
                .FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null || !BC.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Tên đăng nhập hoặc mật khẩu không đúng.");
                return View(model);
            }

            if (user.Role != "Admin" && !user.IsActivated)
            {
                ModelState.AddModelError("", "Tài khoản này chưa được kích hoạt. Vui lòng dùng chức năng đăng ký để xác thực email và tạo mật khẩu.");
                return View(model);
            }

            if (user.IsLocked)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Name, user.Username),
                new(ClaimTypes.GivenName, user.FullName),
                new(ClaimTypes.Role, user.Role)
            };

            if (user.Role == "Student")
            {
                var student = await _db.Students.FirstOrDefaultAsync(s => s.UserId == user.UserId);
                if (student != null)
                    claims.Add(new Claim("StudentId", student.StudentId.ToString()));
            }
            else if (user.Role == "Teacher")
            {
                var teacher = await _db.Teachers.FirstOrDefaultAsync(t => t.UserId == user.UserId);
                if (teacher != null)
                    claims.Add(new Claim("TeacherId", teacher.TeacherId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);
            var authProps = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(7) : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProps);

            if (user.MustChangePassword && user.Role != "Admin")
            {
                TempData["Info"] = "Bạn đang dùng mật khẩu tạm. Vui lòng đổi mật khẩu ngay để bảo vệ tài khoản.";
                return RedirectToAction("ChangePassword");
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToHome();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username && u.Role != "Admin");
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản phù hợp để đăng ký.");
                return View(model);
            }

            if (user.IsActivated)
            {
                ModelState.AddModelError("", "Tài khoản này đã được kích hoạt. Bạn có thể dùng chức năng quên mật khẩu nếu cần.");
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(user.Email)
                && !string.Equals(user.Email.Trim(), model.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Email", "Email không khớp với thông tin tài khoản đã được cấp.");
                return View(model);
            }

            user.Email = model.Email.Trim();
            user.UpdatedAt = DateTime.Now;

            var token = await CreateEmailVerificationTokenAsync(user, "Register", user.Email);
            await _db.SaveChangesAsync();
            try
            {
                await SendVerificationEmailAsync(user.FullName, user.Email!, token.Code, "Xác thực đăng ký tài khoản");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Không gửi được email xác thực: {ex.Message}");
                return View(model);
            }

            TempData["Success"] = "Mã xác thực đã được gửi tới email của bạn.";
            return RedirectToAction("VerifyOtp", new { key = token.Token.VerificationKey, purpose = "Register" });
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username && u.Role != "Admin");

            if (user == null || !user.IsActivated)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản đã kích hoạt phù hợp.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(user.Email))
            {
                ModelState.AddModelError("", "Tài khoản này chưa có email để khôi phục mật khẩu.");
                return View(model);
            }

            if (!string.IsNullOrWhiteSpace(model.Email)
                && !string.Equals(user.Email.Trim(), model.Email.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("Email", "Email không khớp với tài khoản.");
                return View(model);
            }

            var token = await CreateEmailVerificationTokenAsync(user, "ResetPassword", user.Email);
            await _db.SaveChangesAsync();
            try
            {
                await SendVerificationEmailAsync(user.FullName, user.Email, token.Code, "Mã xác thực khôi phục mật khẩu");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Không gửi được email xác thực: {ex.Message}");
                return View(model);
            }

            TempData["Success"] = "Mã xác thực khôi phục mật khẩu đã được gửi tới email của bạn.";
            return RedirectToAction("VerifyOtp", new { key = token.Token.VerificationKey, purpose = "ResetPassword" });
        }

        [HttpGet]
        public async Task<IActionResult> VerifyOtp(string key, string purpose)
        {
            var token = await FindActiveTokenAsync(key, purpose);
            if (token == null)
            {
                TempData["Error"] = "Mã xác thực không còn hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(purpose == "Register" ? "Register" : "ForgotPassword");
            }

            return View(new VerifyOtpViewModel
            {
                VerificationKey = key,
                Purpose = purpose,
                MaskedEmail = MaskEmail(token.Email)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var token = await FindActiveTokenAsync(model.VerificationKey, model.Purpose);
            if (token == null)
            {
                TempData["Error"] = "Mã xác thực không còn hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(model.Purpose == "Register" ? "Register" : "ForgotPassword");
            }

            if (!BC.Verify(model.Code, token.CodeHash))
            {
                token.FailedAttempts += 1;
                await _db.SaveChangesAsync();
                ModelState.AddModelError("Code", "Mã xác thực không đúng.");
                model.MaskedEmail = MaskEmail(token.Email);
                return View(model);
            }

            token.VerifiedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return RedirectToAction("SetPassword", new { key = token.VerificationKey, purpose = model.Purpose });
        }

        [HttpGet]
        public async Task<IActionResult> SetPassword(string key, string purpose)
        {
            var token = await FindVerifiedTokenAsync(key, purpose);
            if (token == null)
            {
                TempData["Error"] = "Phiên xác thực không hợp lệ. Vui lòng yêu cầu mã mới.";
                return RedirectToAction(purpose == "Register" ? "Register" : "ForgotPassword");
            }

            return View(new SetPasswordViewModel
            {
                VerificationKey = key,
                Purpose = purpose
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var token = await FindVerifiedTokenAsync(model.VerificationKey, model.Purpose);
            if (token == null)
            {
                TempData["Error"] = "Phiên xác thực không hợp lệ. Vui lòng yêu cầu mã mới.";
                return RedirectToAction(model.Purpose == "Register" ? "Register" : "ForgotPassword");
            }

            token.User.PasswordHash = BC.HashPassword(model.NewPassword);
            token.User.IsActivated = true;
            token.User.MustChangePassword = false;
            token.User.UpdatedAt = DateTime.Now;
            token.ConsumedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["Success"] = model.Purpose == "Register"
                ? "Kích hoạt tài khoản thành công. Bạn có thể đăng nhập ngay bây giờ."
                : "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới.";

            return RedirectToAction("Login");
        }

        private IActionResult RedirectToHome()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
            return role switch
            {
                "Admin" => RedirectToAction("Index", "Admin"),
                "Teacher" => RedirectToAction("Index", "Teacher"),
                "Student" => RedirectToAction("Index", "Student"),
                _ => RedirectToAction("Index", "Home")
            };
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _db.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            var vm = new ProfileViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role,
            };

            if (user.Student != null)
            {
                vm.StudentCode = user.Student.StudentCode;
                vm.ClassName = user.Student.ClassName;
                vm.DateOfBirth = user.Student.DateOfBirth;
            }
            else if (user.Teacher != null)
            {
                vm.TeacherCode = user.Teacher.TeacherCode;
                vm.Department = user.Teacher.Department;
                vm.Degree = user.Teacher.Degree;
            }

            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(ProfileViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _db.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return NotFound();

            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.UpdatedAt = DateTime.Now;

            if (user.Student != null)
            {
                user.Student.ClassName = model.ClassName;
                user.Student.DateOfBirth = model.DateOfBirth;
            }
            else if (user.Teacher != null)
            {
                user.Teacher.Department = model.Department;
                user.Teacher.Degree = model.Degree;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var user = await _db.Users.FindAsync(userId);

            if (user == null) return NotFound();

            if (!BC.Verify(model.OldPassword, user.PasswordHash))
            {
                ModelState.AddModelError("OldPassword", "Mật khẩu cũ không đúng.");
                return View(model);
            }

            user.PasswordHash = BC.HashPassword(model.NewPassword);
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task<(EmailVerificationToken Token, string Code)> CreateEmailVerificationTokenAsync(User user, string purpose, string email)
        {
            var existingTokens = await _db.EmailVerificationTokens
                .Where(t => t.UserId == user.UserId && t.Purpose == purpose && t.ConsumedAt == null)
                .ToListAsync();

            if (existingTokens.Count > 0)
                _db.EmailVerificationTokens.RemoveRange(existingTokens);

            var code = Random.Shared.Next(100000, 999999).ToString();
            var token = new EmailVerificationToken
            {
                UserId = user.UserId,
                Purpose = purpose,
                Email = email,
                CodeHash = BC.HashPassword(code),
                VerificationKey = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.Now,
                ExpiresAt = DateTime.Now.AddMinutes(10)
            };

            _db.EmailVerificationTokens.Add(token);
            return (token, code);
        }

        private async Task<EmailVerificationToken?> FindActiveTokenAsync(string key, string purpose)
        {
            var now = DateTime.Now;
            return await _db.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.VerificationKey == key
                    && t.Purpose == purpose
                    && t.ConsumedAt == null
                    && t.ExpiresAt > now
                    && t.FailedAttempts < 5);
        }

        private async Task<EmailVerificationToken?> FindVerifiedTokenAsync(string key, string purpose)
        {
            var now = DateTime.Now;
            return await _db.EmailVerificationTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t =>
                    t.VerificationKey == key
                    && t.Purpose == purpose
                    && t.VerifiedAt != null
                    && t.ConsumedAt == null
                    && t.ExpiresAt > now);
        }

        private async Task SendVerificationEmailAsync(string fullName, string email, string code, string title)
        {
            var html = $"""
                <div style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6;color:#0f172a">
                    <h2 style="margin-bottom:8px">{title}</h2>
                    <p>Xin chào <strong>{fullName}</strong>,</p>
                    <p>Mã xác thực của bạn là:</p>
                    <div style="font-size:28px;font-weight:700;letter-spacing:6px;background:#eff6ff;border:1px solid #bfdbfe;border-radius:12px;padding:16px 20px;display:inline-block;color:#1d4ed8">
                        {code}
                    </div>
                    <p style="margin-top:16px">Mã có hiệu lực trong <strong>10 phút</strong>.</p>
                    <p>Nếu bạn không thực hiện thao tác này, hãy bỏ qua email.</p>
                </div>
                """;

            await _emailSender.SendAsync(email, title, html);
        }

        private static string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2 || parts[0].Length <= 2)
                return email;

            return $"{parts[0][0]}***{parts[0][^1]}@{parts[1]}";
        }
    }
}
