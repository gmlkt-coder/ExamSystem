using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.ViewModels;
using BC = BCrypt.Net.BCrypt;

namespace ExamSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ExamDbContext _db;

        public AdminController(ExamDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            ViewBag.TotalUsers = await _db.Users.CountAsync(u => u.Role != "Admin");
            ViewBag.TotalTeachers = await _db.Teachers.CountAsync();
            ViewBag.TotalStudents = await _db.Students.CountAsync();
            ViewBag.TotalExams = await _db.Exams.CountAsync(e => e.ExamType == "Exam");
            return View();
        }

        public async Task<IActionResult> Users(string? role, string? search)
        {
            var query = _db.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .AsQueryable();

            if (!string.IsNullOrEmpty(role))
                query = query.Where(u => u.Role == role);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(u => u.FullName.Contains(search) || u.Username.Contains(search));

            ViewBag.Role = role;
            ViewBag.Search = search;
            return View(await query.OrderBy(u => u.Role).ThenBy(u => u.FullName).ToListAsync());
        }

        [HttpGet]
        public IActionResult CreateTeacher()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(CreateTeacherViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.BulkInput))
                return await CreateTeachersBulk(model);

            if (!ModelState.IsValid) return View(model);

            var result = await CreateTeacherAccountAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(nameof(CreateTeacherViewModel.TeacherCode), result.ErrorMessage!);
                return View(model);
            }

            TempData["Success"] = $"Đã tạo tài khoản giáo viên {model.FullName}. Giáo viên cần vào trang đăng ký để kích hoạt bằng email.";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public IActionResult CreateStudent()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudent(CreateStudentViewModel model)
        {
            if (!string.IsNullOrWhiteSpace(model.BulkInput))
                return await CreateStudentsBulk(model);

            if (!ModelState.IsValid) return View(model);

            var result = await CreateStudentAccountAsync(model);
            if (!result.Success)
            {
                ModelState.AddModelError(nameof(CreateStudentViewModel.StudentCode), result.ErrorMessage!);
                return View(model);
            }

            TempData["Success"] = $"Đã tạo tài khoản sinh viên {model.FullName}. Sinh viên cần vào trang đăng ký để kích hoạt bằng email.";
            return RedirectToAction("Users");
        }

        [HttpGet]
        public async Task<IActionResult> EditUser(int id)
        {
            var user = await _db.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null) return NotFound();

            var vm = new EditUserViewModel
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone
            };

            if (user.Student != null)
            {
                vm.ClassName = user.Student.ClassName;
                vm.DateOfBirth = user.Student.DateOfBirth;
            }
            else if (user.Teacher != null)
            {
                vm.Department = user.Teacher.Department;
                vm.Degree = user.Teacher.Degree;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _db.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .FirstOrDefaultAsync(u => u.UserId == model.UserId);

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
            TempData["Success"] = "Cập nhật tài khoản thành công!";
            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLock(int id)
        {
            var user = await _db.Users.FindAsync(id);
            if (user == null || user.Role == "Admin") return NotFound();

            user.IsLocked = !user.IsLocked;
            user.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            TempData["Success"] = user.IsLocked
                ? $"Đã khóa tài khoản {user.FullName}."
                : $"Đã mở khóa tài khoản {user.FullName}.";

            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await DeleteUsersInternalAsync(new List<int> { id });
            TempData[result.Success ? "Success" : "Error"] = result.Message;

            if (!result.Success && result.NotFound)
                return NotFound();

            return RedirectToAction("Users");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUsers(List<int>? selectedUserIds)
        {
            if (selectedUserIds == null || selectedUserIds.Count == 0)
            {
                TempData["Error"] = "Vui lòng chọn ít nhất 1 tài khoản để xóa.";
                return RedirectToAction("Users");
            }

            var result = await DeleteUsersInternalAsync(selectedUserIds);
            TempData[result.Success ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Users");
        }

        private async Task<IActionResult> CreateTeachersBulk(CreateTeacherViewModel model)
        {
            ModelState.Remove(nameof(CreateTeacherViewModel.InitialPassword));

            var parsedTeachers = ParseTeacherBulkInput(model.BulkInput);
            if (!ModelState.IsValid)
                return View(model);

            if (parsedTeachers.Count == 0)
            {
                ModelState.AddModelError(nameof(CreateTeacherViewModel.BulkInput), "Vui lòng nhập ít nhất 1 dòng giáo viên.");
                return View(model);
            }

            var duplicateCodes = parsedTeachers
                .GroupBy(t => t.TeacherCode, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateCodes.Count > 0)
            {
                ModelState.AddModelError(nameof(CreateTeacherViewModel.BulkInput), $"Trùng mã giáo viên trong danh sách: {string.Join(", ", duplicateCodes)}.");
                return View(model);
            }

            var teacherCodes = parsedTeachers.Select(t => t.TeacherCode).ToList();
            var existingCodes = await _db.Users
                .Where(u => teacherCodes.Contains(u.Username))
                .Select(u => u.Username)
                .ToListAsync();

            if (existingCodes.Count > 0)
            {
                ModelState.AddModelError(nameof(CreateTeacherViewModel.BulkInput), $"Các mã giáo viên đã tồn tại: {string.Join(", ", existingCodes)}.");
                return View(model);
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var teacher in parsedTeachers)
                {
                    var result = await CreateTeacherAccountAsync(teacher, saveImmediately: false);
                    if (!result.Success)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError(nameof(CreateTeacherViewModel.BulkInput), result.ErrorMessage!);
                        return View(model);
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = $"Đã tạo {parsedTeachers.Count} tài khoản giáo viên. Các tài khoản này sẽ tự kích hoạt qua email.";
                return RedirectToAction("Users");
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(nameof(CreateTeacherViewModel.BulkInput), "Lỗi khi tạo danh sách giáo viên. Vui lòng kiểm tra dữ liệu và thử lại.");
                return View(model);
            }
        }

        private async Task<IActionResult> CreateStudentsBulk(CreateStudentViewModel model)
        {
            ModelState.Remove(nameof(CreateStudentViewModel.InitialPassword));

            var parsedStudents = ParseStudentBulkInput(model.BulkInput);
            if (!ModelState.IsValid)
                return View(model);

            if (parsedStudents.Count == 0)
            {
                ModelState.AddModelError(nameof(CreateStudentViewModel.BulkInput), "Vui lòng nhập ít nhất 1 dòng sinh viên.");
                return View(model);
            }

            var duplicateCodes = parsedStudents
                .GroupBy(s => s.StudentCode, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateCodes.Count > 0)
            {
                ModelState.AddModelError(nameof(CreateStudentViewModel.BulkInput), $"Trùng mã sinh viên trong danh sách: {string.Join(", ", duplicateCodes)}.");
                return View(model);
            }

            var studentCodes = parsedStudents.Select(s => s.StudentCode).ToList();
            var existingCodes = await _db.Users
                .Where(u => studentCodes.Contains(u.Username))
                .Select(u => u.Username)
                .ToListAsync();

            if (existingCodes.Count > 0)
            {
                ModelState.AddModelError(nameof(CreateStudentViewModel.BulkInput), $"Các mã sinh viên đã tồn tại: {string.Join(", ", existingCodes)}.");
                return View(model);
            }

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var student in parsedStudents)
                {
                    var result = await CreateStudentAccountAsync(student, saveImmediately: false);
                    if (!result.Success)
                    {
                        await transaction.RollbackAsync();
                        ModelState.AddModelError(nameof(CreateStudentViewModel.BulkInput), result.ErrorMessage!);
                        return View(model);
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                TempData["Success"] = $"Đã tạo {parsedStudents.Count} tài khoản sinh viên. Các tài khoản này sẽ tự kích hoạt qua email.";
                return RedirectToAction("Users");
            }
            catch
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(nameof(CreateStudentViewModel.BulkInput), "Lỗi khi tạo danh sách sinh viên. Vui lòng kiểm tra dữ liệu và thử lại.");
                return View(model);
            }
        }

        private async Task<OperationResult> CreateTeacherAccountAsync(CreateTeacherViewModel model, bool saveImmediately = true)
        {
            if (await _db.Users.AnyAsync(u => u.Username == model.TeacherCode))
                return OperationResult.Fail("Mã giáo viên này đã tồn tại.");

            if (string.IsNullOrWhiteSpace(model.Email))
                return OperationResult.Fail("Email giáo viên là bắt buộc để kích hoạt tài khoản.");

            var user = new User
            {
                Username = model.TeacherCode,
                PasswordHash = BC.HashPassword(Guid.NewGuid().ToString("N")),
                FullName = model.FullName,
                Email = model.Email?.Trim(),
                Phone = model.Phone,
                Role = "Teacher",
                IsActivated = false,
                MustChangePassword = false
            };

            _db.Users.Add(user);
            if (saveImmediately)
                await _db.SaveChangesAsync();

            var teacher = new Teacher
            {
                User = user,
                TeacherCode = model.TeacherCode,
                Department = model.Department,
                Degree = model.Degree
            };

            _db.Teachers.Add(teacher);
            if (saveImmediately)
                await _db.SaveChangesAsync();

            return OperationResult.Ok();
        }

        private async Task<OperationResult> CreateStudentAccountAsync(CreateStudentViewModel model, bool saveImmediately = true)
        {
            if (await _db.Users.AnyAsync(u => u.Username == model.StudentCode))
                return OperationResult.Fail("Mã sinh viên này đã tồn tại.");

            if (string.IsNullOrWhiteSpace(model.Email))
                return OperationResult.Fail("Email sinh viên là bắt buộc để kích hoạt tài khoản.");

            var user = new User
            {
                Username = model.StudentCode,
                PasswordHash = BC.HashPassword(Guid.NewGuid().ToString("N")),
                FullName = model.FullName,
                Email = model.Email?.Trim(),
                Phone = model.Phone,
                Role = "Student",
                IsActivated = false,
                MustChangePassword = false
            };

            _db.Users.Add(user);
            if (saveImmediately)
                await _db.SaveChangesAsync();

            var student = new Student
            {
                User = user,
                StudentCode = model.StudentCode,
                ClassName = model.ClassName,
                DateOfBirth = model.DateOfBirth
            };

            _db.Students.Add(student);
            if (saveImmediately)
                await _db.SaveChangesAsync();

            return OperationResult.Ok();
        }

        private List<CreateTeacherViewModel> ParseTeacherBulkInput(string? bulkInput)
        {
            var result = new List<CreateTeacherViewModel>();
            if (string.IsNullOrWhiteSpace(bulkInput))
                return result;

            var lines = bulkInput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Split('|');
                if (parts.Length < 6)
                {
                    ModelState.AddModelError(nameof(CreateTeacherViewModel.BulkInput), $"Dòng {i + 1} chưa đủ 6 cột theo định dạng mã|họ tên|email|sđt|khoa|học vị.");
                    continue;
                }

                var item = new CreateTeacherViewModel
                {
                    TeacherCode = parts[0].Trim(),
                    FullName = parts[1].Trim(),
                    Email = NormalizeOptional(parts[2]),
                    Phone = NormalizeOptional(parts[3]),
                    Department = NormalizeOptional(parts[4]),
                    Degree = NormalizeOptional(parts[5])
                };

                if (string.IsNullOrWhiteSpace(item.TeacherCode) || string.IsNullOrWhiteSpace(item.FullName) || string.IsNullOrWhiteSpace(item.Email))
                {
                    ModelState.AddModelError(nameof(CreateTeacherViewModel.BulkInput), $"Dòng {i + 1} thiếu mã giáo viên, họ tên hoặc email.");
                    continue;
                }

                result.Add(item);
            }

            return result;
        }

        private List<CreateStudentViewModel> ParseStudentBulkInput(string? bulkInput)
        {
            var result = new List<CreateStudentViewModel>();
            if (string.IsNullOrWhiteSpace(bulkInput))
                return result;

            var lines = bulkInput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var parts = lines[i].Split('|');
                if (parts.Length < 6)
                {
                    ModelState.AddModelError(nameof(CreateStudentViewModel.BulkInput), $"Dòng {i + 1} chưa đủ 6 cột theo định dạng mã|họ tên|email|sđt|lớp|ngày sinh.");
                    continue;
                }

                DateOnly? dateOfBirth = null;
                var rawDate = parts[5].Trim();
                if (!string.IsNullOrWhiteSpace(rawDate) && !TryParseDateOnly(rawDate, out dateOfBirth))
                {
                    ModelState.AddModelError(nameof(CreateStudentViewModel.BulkInput), $"Dòng {i + 1} có ngày sinh không hợp lệ. Dùng `yyyy-MM-dd` hoặc `dd/MM/yyyy`.");
                    continue;
                }

                var item = new CreateStudentViewModel
                {
                    StudentCode = parts[0].Trim(),
                    FullName = parts[1].Trim(),
                    Email = NormalizeOptional(parts[2]),
                    Phone = NormalizeOptional(parts[3]),
                    ClassName = NormalizeOptional(parts[4]),
                    DateOfBirth = dateOfBirth
                };

                if (string.IsNullOrWhiteSpace(item.StudentCode) || string.IsNullOrWhiteSpace(item.FullName) || string.IsNullOrWhiteSpace(item.Email))
                {
                    ModelState.AddModelError(nameof(CreateStudentViewModel.BulkInput), $"Dòng {i + 1} thiếu mã sinh viên, họ tên hoặc email.");
                    continue;
                }

                result.Add(item);
            }

            return result;
        }

        private async Task<DeleteUsersResult> DeleteUsersInternalAsync(List<int> userIds)
        {
            var distinctIds = userIds.Distinct().ToList();
            var users = await _db.Users
                .Include(u => u.Student)
                .Include(u => u.Teacher)
                .Where(u => distinctIds.Contains(u.UserId) && u.Role != "Admin")
                .ToListAsync();

            if (users.Count == 0)
                return DeleteUsersResult.Fail("Không tìm thấy tài khoản hợp lệ để xóa.", true);

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                foreach (var user in users)
                    await DeleteUserDependenciesAsync(user);

                _db.Users.RemoveRange(users);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                var message = users.Count == 1
                    ? $"Đã xóa tài khoản {users[0].FullName}."
                    : $"Đã xóa {users.Count} tài khoản đã chọn.";

                return DeleteUsersResult.Ok(message);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return DeleteUsersResult.Fail($"Không thể xóa tài khoản: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private async Task DeleteUserDependenciesAsync(User user)
        {
            var tokenRows = await _db.EmailVerificationTokens.Where(t => t.UserId == user.UserId).ToListAsync();
            if (tokenRows.Count > 0)
                _db.EmailVerificationTokens.RemoveRange(tokenRows);

            var resetRows = await _db.PasswordResetRequests.Where(t => t.UserId == user.UserId).ToListAsync();
            if (resetRows.Count > 0)
                _db.PasswordResetRequests.RemoveRange(resetRows);

            if (user.Teacher != null)
            {
                var teacherId = user.Teacher.TeacherId;
                var examIds = await _db.Exams
                    .Where(e => e.CreatedByTeacherId == teacherId)
                    .Select(e => e.ExamId)
                    .ToListAsync();

                if (examIds.Count > 0)
                {
                    var questionIds = await _db.Questions
                        .Where(q => examIds.Contains(q.ExamId))
                        .Select(q => q.QuestionId)
                        .ToListAsync();

                    var submissionIds = await _db.ExamSubmissions
                        .Where(es => examIds.Contains(es.ExamId))
                        .Select(es => es.SubmissionId)
                        .ToListAsync();

                    var studentAnswers = await _db.StudentAnswers
                        .Where(sa => submissionIds.Contains(sa.SubmissionId))
                        .ToListAsync();

                    var answers = await _db.Answers
                        .Where(a => questionIds.Contains(a.QuestionId))
                        .ToListAsync();

                    var questions = await _db.Questions
                        .Where(q => questionIds.Contains(q.QuestionId))
                        .ToListAsync();

                    var examSubmissions = await _db.ExamSubmissions
                        .Where(es => submissionIds.Contains(es.SubmissionId))
                        .ToListAsync();

                    var exams = await _db.Exams
                        .Where(e => examIds.Contains(e.ExamId))
                        .ToListAsync();

                    _db.StudentAnswers.RemoveRange(studentAnswers);
                    _db.ExamSubmissions.RemoveRange(examSubmissions);
                    _db.Answers.RemoveRange(answers);
                    _db.Questions.RemoveRange(questions);
                    _db.Exams.RemoveRange(exams);
                }
            }

            if (user.Student != null)
            {
                var studentId = user.Student.StudentId;

                var submissionIds = await _db.ExamSubmissions
                    .Where(es => es.StudentId == studentId)
                    .Select(es => es.SubmissionId)
                    .ToListAsync();

                var studentAnswers = await _db.StudentAnswers
                    .Where(sa => submissionIds.Contains(sa.SubmissionId))
                    .ToListAsync();

                var examSubmissions = await _db.ExamSubmissions
                    .Where(es => submissionIds.Contains(es.SubmissionId))
                    .ToListAsync();

                var practiceExamIds = await _db.PracticeExams
                    .Where(pe => pe.StudentId == studentId)
                    .Select(pe => pe.PracticeExamId)
                    .ToListAsync();

                var practiceQuestionIds = await _db.PracticeQuestions
                    .Where(pq => practiceExamIds.Contains(pq.PracticeExamId))
                    .Select(pq => pq.PracticeQuestionId)
                    .ToListAsync();

                var practiceAnswers = await _db.PracticeAnswers
                    .Where(pa => practiceQuestionIds.Contains(pa.PracticeQuestionId))
                    .ToListAsync();

                var practiceQuestions = await _db.PracticeQuestions
                    .Where(pq => practiceQuestionIds.Contains(pq.PracticeQuestionId))
                    .ToListAsync();

                var practiceSubmissions = await _db.PracticeSubmissions
                    .Where(ps => ps.StudentId == studentId || practiceExamIds.Contains(ps.PracticeExamId))
                    .ToListAsync();

                var practiceExams = await _db.PracticeExams
                    .Where(pe => practiceExamIds.Contains(pe.PracticeExamId))
                    .ToListAsync();

                _db.StudentAnswers.RemoveRange(studentAnswers);
                _db.ExamSubmissions.RemoveRange(examSubmissions);
                _db.PracticeAnswers.RemoveRange(practiceAnswers);
                _db.PracticeQuestions.RemoveRange(practiceQuestions);
                _db.PracticeSubmissions.RemoveRange(practiceSubmissions);
                _db.PracticeExams.RemoveRange(practiceExams);
            }
        }

        private static string? NormalizeOptional(string value)
        {
            var trimmed = value.Trim();
            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
        }

        private static bool TryParseDateOnly(string raw, out DateOnly? value)
        {
            value = null;
            var formats = new[] { "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" };

            foreach (var format in formats)
            {
                if (DateOnly.TryParseExact(raw, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
                {
                    value = parsed;
                    return true;
                }
            }

            return false;
        }

        private record OperationResult(bool Success, string? ErrorMessage = null)
        {
            public static OperationResult Ok() => new(true);
            public static OperationResult Fail(string errorMessage) => new(false, errorMessage);
        }

        private record DeleteUsersResult(bool Success, string Message, bool NotFound = false)
        {
            public static DeleteUsersResult Ok(string message) => new(true, message);
            public static DeleteUsersResult Fail(string message, bool notFound = false) => new(false, message, notFound);
        }
    }
}
