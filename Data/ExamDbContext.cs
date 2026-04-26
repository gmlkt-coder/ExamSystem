using Microsoft.EntityFrameworkCore;
using ExamSystem.Models;

namespace ExamSystem.Data
{
    public class ExamDbContext : DbContext
    {
        public ExamDbContext(DbContextOptions<ExamDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Teacher> Teachers => Set<Teacher>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<Answer> Answers => Set<Answer>();
        public DbSet<ExamSubmission> ExamSubmissions => Set<ExamSubmission>();
        public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
        public DbSet<PracticeExam> PracticeExams => Set<PracticeExam>();
        public DbSet<PracticeQuestion> PracticeQuestions => Set<PracticeQuestion>();
        public DbSet<PracticeAnswer> PracticeAnswers => Set<PracticeAnswer>();
        public DbSet<PracticeSubmission> PracticeSubmissions => Set<PracticeSubmission>();
        public DbSet<PasswordResetRequest> PasswordResetRequests => Set<PasswordResetRequest>();
        public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username).IsUnique();

            // Student -> User (1-1)
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.Student)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student>()
                .HasIndex(s => s.StudentCode).IsUnique();

            // Teacher -> User (1-1)
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.Teacher)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Teacher>()
                .HasIndex(t => t.TeacherCode).IsUnique();

            // Exam -> Teacher
            modelBuilder.Entity<Exam>()
                .HasOne(e => e.Teacher)
                .WithMany(t => t.Exams)
                .HasForeignKey(e => e.CreatedByTeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Question -> Exam
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Exam)
                .WithMany(e => e.Questions)
                .HasForeignKey(q => q.ExamId)
                .OnDelete(DeleteBehavior.Cascade);

            // Answer -> Question
            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ExamSubmission
            modelBuilder.Entity<ExamSubmission>()
                .HasOne(es => es.Exam)
                .WithMany(e => e.Submissions)
                .HasForeignKey(es => es.ExamId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamSubmission>()
                .HasOne(es => es.Student)
                .WithMany(s => s.Submissions)
                .HasForeignKey(es => es.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ExamSubmission>()
                .HasIndex(es => new { es.ExamId, es.StudentId }).IsUnique();

            // StudentAnswer
            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Submission)
                .WithMany(s => s.StudentAnswers)
                .HasForeignKey(sa => sa.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Question)
                .WithMany(q => q.StudentAnswers)
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.SelectedAnswer)
                .WithMany()
                .HasForeignKey(sa => sa.SelectedAnswerId)
                .OnDelete(DeleteBehavior.SetNull);

            // PracticeExam -> Student
            modelBuilder.Entity<PracticeExam>()
                .HasOne(pe => pe.Student)
                .WithMany(s => s.PracticeExams)
                .HasForeignKey(pe => pe.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // PasswordResetRequest -> User
            modelBuilder.Entity<PasswordResetRequest>()
                .HasOne(pr => pr.User)
                .WithMany(u => u.PasswordResetRequests)
                .HasForeignKey(pr => pr.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EmailVerificationToken>()
                .HasOne(t => t.User)
                .WithMany(u => u.EmailVerificationTokens)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Decimal precision
            modelBuilder.Entity<Exam>().Property(e => e.MaxScore).HasPrecision(5, 2);
            modelBuilder.Entity<Question>().Property(q => q.Points).HasPrecision(5, 2);
            modelBuilder.Entity<ExamSubmission>().Property(es => es.TotalScore).HasPrecision(5, 2);
            modelBuilder.Entity<StudentAnswer>().Property(sa => sa.ScoreEarned).HasPrecision(5, 2);
            modelBuilder.Entity<PracticeQuestion>().Property(pq => pq.Points).HasPrecision(5, 2);
            modelBuilder.Entity<PracticeSubmission>().Property(ps => ps.TotalScore).HasPrecision(5, 2);
            modelBuilder.Entity<PracticeSubmission>().Property(ps => ps.MaxScore).HasPrecision(5, 2);
        }
    }
}
