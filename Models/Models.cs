using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExamSystem.Models
{
    // ============================================
    // USER MODEL
    // ============================================
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(50)]
        public string Username { get; set; } = "";

        [Required, MaxLength(256)]
        public string PasswordHash { get; set; } = "";

        [Required, MaxLength(100)]
        public string FullName { get; set; } = "";

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [Required, MaxLength(20)]
        public string Role { get; set; } = "Student";  // Admin, Teacher, Student

        public bool IsLocked { get; set; } = false;
        public bool IsActivated { get; set; } = false;
        public bool MustChangePassword { get; set; } = false;
        [MaxLength(256)]
        public string? RecoveryCodeHash { get; set; }
        public DateTime? RecoveryCodeUpdatedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        public Student? Student { get; set; }
        public Teacher? Teacher { get; set; }
        public ICollection<PasswordResetRequest> PasswordResetRequests { get; set; } = new List<PasswordResetRequest>();
        public ICollection<EmailVerificationToken> EmailVerificationTokens { get; set; } = new List<EmailVerificationToken>();
    }

    // ============================================
    // STUDENT MODEL
    // ============================================
    public class Student
    {
        [Key]
        public int StudentId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(20)]
        public string StudentCode { get; set; } = "";

        [MaxLength(50)]
        public string? ClassName { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        public ICollection<ExamSubmission> Submissions { get; set; } = new List<ExamSubmission>();
        public ICollection<PracticeExam> PracticeExams { get; set; } = new List<PracticeExam>();
    }

    // ============================================
    // TEACHER MODEL
    // ============================================
    public class Teacher
    {
        [Key]
        public int TeacherId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(20)]
        public string TeacherCode { get; set; } = "";

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(50)]
        public string? Degree { get; set; }

        // Navigation
        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
        public ICollection<Exam> Exams { get; set; } = new List<Exam>();
    }

    // ============================================
    // EXAM MODEL
    // ============================================
    public class Exam
    {
        [Key]
        public int ExamId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        [MaxLength(100)]
        public string? Subject { get; set; }

        [Required]
        public int Duration { get; set; }  // minutes

        [MaxLength(100)]
        public string? Password { get; set; }

        public decimal MaxScore { get; set; } = 10.0m;

        public bool IsPublished { get; set; } = false;

        public DateTime? PublishedAt { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public bool AllowReview { get; set; } = false;

        public int CreatedByTeacherId { get; set; }

        [MaxLength(20)]
        public string ExamType { get; set; } = "Exam";

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("CreatedByTeacherId")]
        public Teacher Teacher { get; set; } = null!;
        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<ExamSubmission> Submissions { get; set; } = new List<ExamSubmission>();
    }

    // ============================================
    // QUESTION MODEL
    // ============================================
    public class Question
    {
        [Key]
        public int QuestionId { get; set; }

        public int ExamId { get; set; }

        [Required]
        public string QuestionText { get; set; } = "";

        [Required, MaxLength(20)]
        public string QuestionType { get; set; } = "MultipleChoice"; // MultipleChoice, TrueFalse, Essay

        public decimal Points { get; set; } = 1.0m;

        public int OrderIndex { get; set; } = 0;

        public string? Explanation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("ExamId")]
        public Exam Exam { get; set; } = null!;
        public ICollection<Answer> Answers { get; set; } = new List<Answer>();
        public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }

    // ============================================
    // ANSWER MODEL
    // ============================================
    public class Answer
    {
        [Key]
        public int AnswerId { get; set; }

        public int QuestionId { get; set; }

        [Required]
        public string AnswerText { get; set; } = "";

        public bool IsCorrect { get; set; } = false;

        public int OrderIndex { get; set; } = 0;

        // Navigation
        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;
    }

    // ============================================
    // EXAM SUBMISSION MODEL
    // ============================================
    public class ExamSubmission
    {
        [Key]
        public int SubmissionId { get; set; }

        public int ExamId { get; set; }
        public int StudentId { get; set; }

        public DateTime StartedAt { get; set; } = DateTime.Now;
        public DateTime? SubmittedAt { get; set; }

        public bool IsAutoSubmit { get; set; } = false;

        public decimal? TotalScore { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "InProgress"; // InProgress, Submitted, Graded

        public string? TeacherComment { get; set; }

        // Navigation
        [ForeignKey("ExamId")]
        public Exam Exam { get; set; } = null!;
        [ForeignKey("StudentId")]
        public Student Student { get; set; } = null!;
        public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    }

    // ============================================
    // STUDENT ANSWER MODEL
    // ============================================
    public class StudentAnswer
    {
        [Key]
        public int StudentAnswerId { get; set; }

        public int SubmissionId { get; set; }
        public int QuestionId { get; set; }
        public int? SelectedAnswerId { get; set; }
        public string? EssayAnswer { get; set; }
        public bool? IsCorrect { get; set; }
        public decimal? ScoreEarned { get; set; }
        public DateTime? GradedAt { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("SubmissionId")]
        public ExamSubmission Submission { get; set; } = null!;
        [ForeignKey("QuestionId")]
        public Question Question { get; set; } = null!;
        [ForeignKey("SelectedAnswerId")]
        public Answer? SelectedAnswer { get; set; }
    }

    // ============================================
    // PRACTICE EXAM MODEL
    // ============================================
    public class PracticeExam
    {
        [Key]
        public int PracticeExamId { get; set; }

        public int StudentId { get; set; }

        [Required, MaxLength(200)]
        public string Title { get; set; } = "";

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        // Navigation
        [ForeignKey("StudentId")]
        public Student Student { get; set; } = null!;
        public ICollection<PracticeQuestion> Questions { get; set; } = new List<PracticeQuestion>();
        public ICollection<PracticeSubmission> Submissions { get; set; } = new List<PracticeSubmission>();
    }

    // ============================================
    // PRACTICE QUESTION MODEL
    // ============================================
    public class PracticeQuestion
    {
        [Key]
        public int PracticeQuestionId { get; set; }

        public int PracticeExamId { get; set; }

        [Required]
        public string QuestionText { get; set; } = "";

        [MaxLength(20)]
        public string QuestionType { get; set; } = "MultipleChoice";

        public decimal Points { get; set; } = 1.0m;
        public int OrderIndex { get; set; } = 0;
        public string? Explanation { get; set; }

        // Navigation
        [ForeignKey("PracticeExamId")]
        public PracticeExam PracticeExam { get; set; } = null!;
        public ICollection<PracticeAnswer> Answers { get; set; } = new List<PracticeAnswer>();
    }

    // ============================================
    // PRACTICE ANSWER MODEL
    // ============================================
    public class PracticeAnswer
    {
        [Key]
        public int PracticeAnswerId { get; set; }

        public int PracticeQuestionId { get; set; }

        [Required]
        public string AnswerText { get; set; } = "";

        public bool IsCorrect { get; set; } = false;
        public int OrderIndex { get; set; } = 0;

        [ForeignKey("PracticeQuestionId")]
        public PracticeQuestion PracticeQuestion { get; set; } = null!;
    }

    // ============================================
    // PRACTICE SUBMISSION MODEL
    // ============================================
    public class PracticeSubmission
    {
        [Key]
        public int PracticeSubmissionId { get; set; }

        public int PracticeExamId { get; set; }
        public int StudentId { get; set; }

        public decimal? TotalScore { get; set; }
        public decimal? MaxScore { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.Now;

        [ForeignKey("PracticeExamId")]
        public PracticeExam PracticeExam { get; set; } = null!;
        [ForeignKey("StudentId")]
        public Student Student { get; set; } = null!;
    }

    // ============================================
    // PASSWORD RESET REQUEST MODEL
    // ============================================
    public class PasswordResetRequest
    {
        [Key]
        public int PasswordResetRequestId { get; set; }

        public int UserId { get; set; }

        [MaxLength(20)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Rejected

        [MaxLength(100)]
        public string? Email { get; set; }

        public string? Message { get; set; }

        public string? AdminNote { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public DateTime? ProcessedAt { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }

    // ============================================
    // EMAIL VERIFICATION TOKEN MODEL
    // ============================================
    public class EmailVerificationToken
    {
        [Key]
        public int EmailVerificationTokenId { get; set; }

        public int UserId { get; set; }

        [Required, MaxLength(20)]
        public string Purpose { get; set; } = "Register"; // Register, ResetPassword

        [Required, MaxLength(256)]
        public string CodeHash { get; set; } = "";

        [Required, MaxLength(100)]
        public string Email { get; set; } = "";

        [Required, MaxLength(64)]
        public string VerificationKey { get; set; } = Guid.NewGuid().ToString("N");

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? VerifiedAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public int FailedAttempts { get; set; } = 0;

        [ForeignKey("UserId")]
        public User User { get; set; } = null!;
    }
}
