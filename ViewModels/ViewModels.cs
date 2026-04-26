using ExamSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ExamSystem.ViewModels
{
    // ============================================
    // AUTH VIEW MODELS
    // ============================================
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = "";

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu cũ")]
        public string OldPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mã khôi phục")]
        [Display(Name = "Mã khôi phục")]
        public string RecoveryCode { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateOnly? DateOfBirth { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class VerifyOtpViewModel
    {
        [Required]
        public string VerificationKey { get; set; } = "";

        [Required]
        public string Purpose { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mã xác thực")]
        [Display(Name = "Mã xác thực")]
        public string Code { get; set; } = "";

        public string MaskedEmail { get; set; } = "";
    }

    public class SetPasswordViewModel
    {
        [Required]
        public string VerificationKey { get; set; } = "";

        [Required]
        public string Purpose { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất 6 ký tự")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu")]
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string ConfirmPassword { get; set; } = "";
    }

    public class RecoveryCodeViewModel
    {
        public string Username { get; set; } = "";
        public string RecoveryCode { get; set; } = "";
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
    }

    // ============================================
    // PROFILE VIEW MODEL
    // ============================================
    public class ProfileViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";

        [Required(ErrorMessage = "Vui long nhap ho ten")]
        [Display(Name = "Ho va ten")]
        public string FullName { get; set; } = "";

        [EmailAddress(ErrorMessage = "Email khong hop le")]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "So dien thoai")]
        public string? Phone { get; set; }

        public string Role { get; set; } = "";

        public string? StudentCode { get; set; }
        public string? ClassName { get; set; }
        public DateOnly? DateOfBirth { get; set; }

        public string? TeacherCode { get; set; }
        public string? Department { get; set; }
        public string? Degree { get; set; }
    }

    // ============================================
    // ADMIN - USER MANAGEMENT VIEW MODELS
    // ============================================
    public class CreateTeacherViewModel
    {
        [Required(ErrorMessage = "Mã giáo viên là bắt buộc")]
        [Display(Name = "Mã giáo viên")]
        public string TeacherCode { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required(ErrorMessage = "Số điện thoại là bắt buộc để tự kích hoạt tài khoản")]
        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Khoa/Bộ môn")]
        public string? Department { get; set; }

        [Display(Name = "Học vị")]
        public string? Degree { get; set; }

        [Display(Name = "Mật khẩu ban đầu")]
        public string InitialPassword { get; set; } = "";

        [Display(Name = "Danh sách giáo viên")]
        public string? BulkInput { get; set; }
    }

    public class CreateStudentViewModel
    {
        [Required(ErrorMessage = "Mã sinh viên là bắt buộc")]
        [Display(Name = "Mã sinh viên")]
        public string StudentCode { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng nhập họ và tên")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = "";

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "Số điện thoại")]
        public string? Phone { get; set; }

        [Display(Name = "Lớp")]
        public string? ClassName { get; set; }

        [Display(Name = "Ngày sinh")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Ngày sinh là bắt buộc để tự kích hoạt tài khoản")]
        public DateOnly? DateOfBirth { get; set; }

        [Display(Name = "Mật khẩu ban đầu")]
        public string InitialPassword { get; set; } = "";

        [Display(Name = "Danh sách sinh viên")]
        public string? BulkInput { get; set; }
    }

    public class PasswordResetAdminViewModel
    {
        public int PasswordResetRequestId { get; set; }

        [Required(ErrorMessage = "Vui long nhap mat khau tam")]
        [MinLength(6, ErrorMessage = "Mat khau phai co it nhat 6 ky tu")]
        [Display(Name = "Mat khau tam")]
        public string TemporaryPassword { get; set; } = "";

        [Display(Name = "Ghi chu admin")]
        public string? AdminNote { get; set; }
    }

    public class EditUserViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = "";
        public string Role { get; set; } = "";

        [Required]
        [Display(Name = "Ho va ten")]
        public string FullName { get; set; } = "";

        [EmailAddress]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Display(Name = "So dien thoai")]
        public string? Phone { get; set; }

        public string? ClassName { get; set; }
        public DateOnly? DateOfBirth { get; set; }

        public string? Department { get; set; }
        public string? Degree { get; set; }
    }

    // ============================================
    // EXAM VIEW MODELS
    // ============================================
    public class CreateExamViewModel
    {
        [Required(ErrorMessage = "Tieu de khong duoc de trong")]
        [Display(Name = "Tieu de de thi")]
        public string Title { get; set; } = "";

        [Display(Name = "Mo ta")]
        public string? Description { get; set; }

        [Display(Name = "Mon hoc")]
        public string? Subject { get; set; }

        [Required(ErrorMessage = "Thoi gian lam bai la bat buoc")]
        [Range(1, 300, ErrorMessage = "Thoi gian phai tu 1-300 phut")]
        [Display(Name = "Thoi gian lam bai (phut)")]
        public int Duration { get; set; } = 60;

        [Display(Name = "Mat khau bai thi")]
        public string? Password { get; set; }

        [Display(Name = "Diem toi da")]
        public decimal MaxScore { get; set; } = 10.0m;

        [Display(Name = "Cho phep xem lai")]
        public bool AllowReview { get; set; } = false;

        [Display(Name = "Thoi gian bat dau")]
        [DataType(DataType.DateTime)]
        public DateTime? StartTime { get; set; }

        [Display(Name = "Thoi gian ket thuc")]
        [DataType(DataType.DateTime)]
        public DateTime? EndTime { get; set; }
    }

    public class ExamListItemViewModel
    {
        public int ExamId { get; set; }
        public string Title { get; set; } = "";
        public string? Subject { get; set; }
        public int Duration { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public int QuestionCount { get; set; }
        public string TeacherName { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    // ============================================
    // QUESTION VIEW MODELS
    // ============================================
    public class AddQuestionViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = "";

        [Required(ErrorMessage = "Noi dung cau hoi khong duoc de trong")]
        [Display(Name = "Noi dung cau hoi")]
        public string QuestionText { get; set; } = "";

        [Required]
        [Display(Name = "Loai cau hoi")]
        public string QuestionType { get; set; } = "MultipleChoice";

        [Range(0.1, 100)]
        [Display(Name = "Diem")]
        public decimal Points { get; set; } = 1.0m;

        [Display(Name = "Giai thich")]
        public string? Explanation { get; set; }

        public List<AnswerInputViewModel> Answers { get; set; } = new();
    }

    public class AnswerInputViewModel
    {
        public string AnswerText { get; set; } = "";
        public bool IsCorrect { get; set; } = false;
    }

    // ============================================
    // TAKING EXAM VIEW MODELS
    // ============================================
    public class ExamPasswordViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = "";
        public string? Subject { get; set; }
        public int Duration { get; set; }
        public int QuestionCount { get; set; }

        [Required(ErrorMessage = "Vui long nhap mat khau bai thi")]
        [Display(Name = "Mat khau bai thi")]
        public string Password { get; set; } = "";
    }

    public class TakeExamViewModel
    {
        public int ExamId { get; set; }
        public int SubmissionId { get; set; }
        public string ExamTitle { get; set; } = "";
        public string? Subject { get; set; }
        public int Duration { get; set; }
        public DateTime StartedAt { get; set; }
        public List<QuestionViewModel> Questions { get; set; } = new();
    }

    public class QuestionViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = "";
        public decimal Points { get; set; }
        public int OrderIndex { get; set; }
        public List<AnswerViewModel> Answers { get; set; } = new();
        public int? SelectedAnswerId { get; set; }
        public string? EssayAnswer { get; set; }
    }

    public class AnswerViewModel
    {
        public int AnswerId { get; set; }
        public string AnswerText { get; set; } = "";
    }

    // ============================================
    // RESULTS VIEW MODELS
    // ============================================
    public class ExamResultViewModel
    {
        public int SubmissionId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = "";
        public string? Subject { get; set; }
        public decimal? TotalScore { get; set; }
        public decimal MaxScore { get; set; }
        public string Status { get; set; } = "";
        public DateTime? SubmittedAt { get; set; }
        public string? TeacherComment { get; set; }
        public bool AllowReview { get; set; }
        public List<QuestionResultViewModel> Questions { get; set; } = new();
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
    }

    public class QuestionResultViewModel
    {
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public string QuestionType { get; set; } = "";
        public decimal Points { get; set; }
        public decimal? ScoreEarned { get; set; }
        public bool? IsCorrect { get; set; }
        public string? EssayAnswer { get; set; }
        public string? Explanation { get; set; }
        public List<AnswerResultViewModel> Answers { get; set; } = new();
        public int? SelectedAnswerId { get; set; }
    }

    public class AnswerResultViewModel
    {
        public int AnswerId { get; set; }
        public string AnswerText { get; set; } = "";
        public bool IsCorrect { get; set; }
    }

    // ============================================
    // GRADING VIEW MODEL
    // ============================================
    public class GradeSubmissionViewModel
    {
        public int SubmissionId { get; set; }
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string StudentCode { get; set; } = "";
        public decimal? AutoScore { get; set; }
        public decimal MaxScore { get; set; }
        public string? TeacherComment { get; set; }
        public List<EssayGradeViewModel> EssayQuestions { get; set; } = new();
    }

    public class EssayGradeViewModel
    {
        public int StudentAnswerId { get; set; }
        public int QuestionId { get; set; }
        public string QuestionText { get; set; } = "";
        public decimal Points { get; set; }
        public string? EssayAnswer { get; set; }
        public decimal? ScoreEarned { get; set; }
    }

    // ============================================
    // STATISTICS VIEW MODELS
    // ============================================
    public class ClassStatisticsViewModel
    {
        public int ExamId { get; set; }
        public string ExamTitle { get; set; } = "";
        public int TotalStudents { get; set; }
        public int SubmittedCount { get; set; }
        public decimal? AverageScore { get; set; }
        public decimal? HighestScore { get; set; }
        public decimal? LowestScore { get; set; }
        public List<StudentResultSummary> StudentResults { get; set; } = new();
    }

    public class StudentResultSummary
    {
        public string StudentName { get; set; } = "";
        public string StudentCode { get; set; } = "";
        public string? ClassName { get; set; }
        public decimal? TotalScore { get; set; }
        public string Status { get; set; } = "";
        public DateTime? SubmittedAt { get; set; }
    }

    // ============================================
    // PRACTICE EXAM VIEW MODELS
    // ============================================
    public class CreatePracticeExamViewModel
    {
        [Required(ErrorMessage = "Tieu de khong duoc de trong")]
        [Display(Name = "Tieu de")]
        public string Title { get; set; } = "";

        [Display(Name = "Mo ta")]
        public string? Description { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public string StudentName { get; set; } = "";
        public string StudentCode { get; set; } = "";
        public decimal? AverageScore { get; set; }
        public int TotalExamsTaken { get; set; }
        public List<ExamSubmission> RecentSubmissions { get; set; } = new();
        public List<ExamListItemViewModel> AvailableExams { get; set; } = new();
    }
}
