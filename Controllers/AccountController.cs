using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
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

        public AccountController(ExamDbContext db, IEmailSender emailSender)
        {
            _db = db;
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

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);

            if (user == null || !BC.Verify(model.Password, user.PasswordHash))
            {
                ModelState.AddModelError("", "Ten dang nhap hoac mat khau khong dung.");
                return View(model);
            }

            if (user.Role != "Admin" && !user.IsActivated)
            {
                ModelState.AddModelError("", "Tai khoan nay chua duoc kich hoat. Vui long dung trang dang ky de tu dat mat khau.");
                return View(model);
            }

            if (user.IsLocked)
            {
                ModelState.AddModelError("", "Tai khoan cua ban da bi khoa. Vui long lien he quan tri vien.");
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
                TempData["Info"] = "Ban dang dung mat khau tam. Vui long doi mat khau ngay.";
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

            var user = await _db.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.Username == model.Username && u.Role != "Admin");

            if (user == null)
            {
                ModelState.AddModelError("", "Khong tim thay tai khoan phu hop de kich hoat.");
                return View(model);
            }

            if (user.IsActivated)
            {
                ModelState.AddModelError("", "Tai khoan nay da duoc kich hoat.");
                return View(model);
            }

            if (!string.Equals(NormalizeText(user.FullName), NormalizeText(model.FullName), StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError("FullName", "Ho va ten khong khop voi du lieu tai khoan.");
                return View(model);
            }

            if (!ValidateOwnershipProof(user, model.Phone, model.DateOfBirth))
                return View(model);

            user.PasswordHash = BC.HashPassword(model.NewPassword);
            user.IsActivated = true;
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.Now;

            var recoveryCode = GenerateRecoveryCode();
            user.RecoveryCodeHash = BC.HashPassword(recoveryCode);
            user.RecoveryCodeUpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return View("RecoveryCode", new RecoveryCodeViewModel
            {
                Username = user.Username,
                RecoveryCode = recoveryCode,
                Title = "Kich hoat tai khoan thanh cong",
                Description = "Day la ma khoi phuc duy nhat de tu dat lai mat khau khi quen. He thong chi hien thi ma nay mot lan."
            });
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
                ModelState.AddModelError("", "Khong tim thay tai khoan da kich hoat phu hop.");
                return View(model);
            }

            if (string.IsNullOrWhiteSpace(user.RecoveryCodeHash) || !BC.Verify(model.RecoveryCode.Trim(), user.RecoveryCodeHash))
            {
                ModelState.AddModelError("RecoveryCode", "Ma khoi phuc khong dung.");
                return View(model);
            }

            user.PasswordHash = BC.HashPassword(model.NewPassword);
            user.MustChangePassword = false;
            user.UpdatedAt = DateTime.Now;

            var recoveryCode = GenerateRecoveryCode();
            user.RecoveryCodeHash = BC.HashPassword(recoveryCode);
            user.RecoveryCodeUpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            return View("RecoveryCode", new RecoveryCodeViewModel
            {
                Username = user.Username,
                RecoveryCode = recoveryCode,
                Title = "Dat lai mat khau thanh cong",
                Description = "Ma khoi phuc cu da het hieu luc. Hay luu ma moi nay de tu dat lai mat khau trong lan sau."
            });
        }

        [HttpGet]
        public IActionResult VerifyOtp(string key, string purpose)
        {
            TempData["Error"] = "Chuc nang xac thuc qua email da duoc tat. Vui long dung kich hoat tai khoan hoac ma khoi phuc.";
            return RedirectToAction(purpose == "Register" ? "Register" : "ForgotPassword");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VerifyOtp(VerifyOtpViewModel model)
        {
            TempData["Error"] = "Chuc nang xac thuc qua email da duoc tat. Vui long dung kich hoat tai khoan hoac ma khoi phuc.";
            return RedirectToAction(model.Purpose == "Register" ? "Register" : "ForgotPassword");
        }

        [HttpGet]
        public IActionResult SetPassword(string key, string purpose)
        {
            TempData["Error"] = "Chuc nang dat mat khau qua email da duoc tat. Vui long quay lai va dung luong moi.";
            return RedirectToAction(purpose == "Register" ? "Register" : "ForgotPassword");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetPassword(SetPasswordViewModel model)
        {
            TempData["Error"] = "Chuc nang dat mat khau qua email da duoc tat. Vui long quay lai va dung luong moi.";
            return RedirectToAction(model.Purpose == "Register" ? "Register" : "ForgotPassword");
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
            TempData["Success"] = "Cap nhat thong tin thanh cong.";
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
                ModelState.AddModelError("OldPassword", "Mat khau cu khong dung.");
                return View(model);
            }

            user.PasswordHash = BC.HashPassword(model.NewPassword);
            user.MustChangePassword = false;
            var recoveryCode = GenerateRecoveryCode();
            user.RecoveryCodeHash = BC.HashPassword(recoveryCode);
            user.RecoveryCodeUpdatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return View("RecoveryCode", new RecoveryCodeViewModel
            {
                Username = user.Username,
                RecoveryCode = recoveryCode,
                Title = "Doi mat khau thanh cong",
                Description = "He thong da cap ma khoi phuc moi sau khi doi mat khau. Ma cu khong con hieu luc."
            });
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private bool ValidateOwnershipProof(User user, string? phone, DateOnly? dateOfBirth)
        {
            if (user.Role == "Teacher")
            {
                if (string.IsNullOrWhiteSpace(user.Phone))
                {
                    ModelState.AddModelError("", "Tai khoan giao vien nay chua co so dien thoai doi chieu. Admin can cap nhat so dien thoai truoc.");
                    return false;
                }

                if (!string.Equals(NormalizePhone(user.Phone), NormalizePhone(phone), StringComparison.Ordinal))
                {
                    ModelState.AddModelError("Phone", "So dien thoai khong khop voi du lieu tai khoan.");
                    return false;
                }

                return true;
            }

            if (user.Role == "Student")
            {
                var dob = user.Student?.DateOfBirth;
                if (dob == null)
                {
                    ModelState.AddModelError("", "Tai khoan sinh vien nay chua co ngay sinh doi chieu. Admin can cap nhat ngay sinh truoc.");
                    return false;
                }

                if (dateOfBirth == null || dob.Value != dateOfBirth.Value)
                {
                    ModelState.AddModelError("DateOfBirth", "Ngay sinh khong khop voi du lieu tai khoan.");
                    return false;
                }

                return true;
            }

            ModelState.AddModelError("", "Khong ho tro tu kich hoat tai khoan nay.");
            return false;
        }

        private static string GenerateRecoveryCode()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            Span<char> buffer = stackalloc char[14];
            for (var i = 0; i < buffer.Length; i++)
            {
                buffer[i] = i is 4 or 9 ? '-' : alphabet[Random.Shared.Next(alphabet.Length)];
            }

            return new string(buffer);
        }

        private static string NormalizePhone(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var builder = new StringBuilder(value.Length);
            foreach (var c in value)
            {
                if (char.IsDigit(c))
                    builder.Append(c);
            }

            return builder.ToString();
        }

        private static string NormalizeText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
