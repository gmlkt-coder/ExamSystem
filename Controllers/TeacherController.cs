using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.ViewModels;

namespace ExamSystem.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ExamDbContext _db;

        public TeacherController(ExamDbContext db)
        {
            _db = db;
        }

        private int GetTeacherId()
        {
            return int.Parse(User.FindFirst("TeacherId")!.Value);
        }

        // ============================================
        // DASHBOARD
        // ============================================
        public async Task<IActionResult> Index()
        {
            var teacherId = GetTeacherId();
            ViewBag.TotalExams = await _db.Exams.CountAsync(e => e.CreatedByTeacherId == teacherId && e.ExamType == "Exam");
            ViewBag.PublishedExams = await _db.Exams.CountAsync(e => e.CreatedByTeacherId == teacherId && e.IsPublished);
            ViewBag.TotalSubmissions = await _db.ExamSubmissions
                .Include(es => es.Exam)
                .CountAsync(es => es.Exam.CreatedByTeacherId == teacherId);
            ViewBag.PendingGrading = await _db.ExamSubmissions
                .Include(es => es.Exam)
                .CountAsync(es => es.Exam.CreatedByTeacherId == teacherId && es.Status == "Submitted");
            return View();
        }

        // ============================================
        // EXAM MANAGEMENT
        // ============================================
        public async Task<IActionResult> Exams()
        {
            var teacherId = GetTeacherId();
            var exams = await _db.Exams
                .Include(e => e.Questions)
                .Where(e => e.CreatedByTeacherId == teacherId && e.ExamType == "Exam")
                .OrderByDescending(e => e.CreatedAt)
                .Select(e => new ExamListItemViewModel
                {
                    ExamId = e.ExamId,
                    Title = e.Title,
                    Subject = e.Subject,
                    Duration = e.Duration,
                    IsPublished = e.IsPublished,
                    StartTime = e.StartTime,
                    EndTime = e.EndTime,
                    QuestionCount = e.Questions.Count,
                    CreatedAt = e.CreatedAt
                })
                .ToListAsync();

            return View(exams);
        }

        // ============================================
        // CREATE EXAM
        // ============================================
        [HttpGet]
        public IActionResult CreateExam()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateExam(CreateExamViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var exam = new Exam
            {
                Title = model.Title,
                Description = model.Description,
                Subject = model.Subject,
                Duration = model.Duration,
                Password = model.Password,
                MaxScore = model.MaxScore,
                AllowReview = model.AllowReview,
                StartTime = model.StartTime,
                EndTime = model.EndTime,
                CreatedByTeacherId = GetTeacherId(),
                ExamType = "Exam"
            };

            _db.Exams.Add(exam);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Tạo đề thi thành công!";
            return RedirectToAction("EditExam", new { id = exam.ExamId });
        }

        // ============================================
        // EDIT EXAM INFO
        // ============================================
        [HttpGet]
        public async Task<IActionResult> EditExam(int id)
        {
            var teacherId = GetTeacherId();
            var exam = await _db.Exams
                .Include(e => e.Questions)
                    .ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(e => e.ExamId == id && e.CreatedByTeacherId == teacherId);

            if (exam == null) return NotFound();

            ViewBag.Exam = exam;
            var vm = new CreateExamViewModel
            {
                Title = exam.Title,
                Description = exam.Description,
                Subject = exam.Subject,
                Duration = exam.Duration,
                Password = exam.Password,
                MaxScore = exam.MaxScore,
                AllowReview = exam.AllowReview,
                StartTime = exam.StartTime,
                EndTime = exam.EndTime
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExam(int id, CreateExamViewModel model)
        {
            var teacherId = GetTeacherId();
            var exam = await _db.Exams
                .Include(e => e.Questions).ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(e => e.ExamId == id && e.CreatedByTeacherId == teacherId);

            if (exam == null) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.Exam = exam;
                return View(model);
            }

            exam.Title = model.Title;
            exam.Description = model.Description;
            exam.Subject = model.Subject;
            exam.Duration = model.Duration;
            exam.Password = model.Password;
            exam.MaxScore = model.MaxScore;
            exam.AllowReview = model.AllowReview;
            exam.StartTime = model.StartTime;
            exam.EndTime = model.EndTime;
            exam.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            ViewBag.Exam = exam;
            TempData["Success"] = "Cập nhật đề thi thành công!";
            return View(model);
        }

        // ============================================
        // DELETE EXAM
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExam(int id)
        {
            var teacherId = GetTeacherId();
            var exam = await _db.Exams
                .FirstOrDefaultAsync(e => e.ExamId == id && e.CreatedByTeacherId == teacherId);

            if (exam == null) return NotFound();

            _db.Exams.Remove(exam);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xóa đề thi.";
            return RedirectToAction("Exams");
        }

        // ============================================
        // PUBLISH / UNPUBLISH EXAM
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublish(int id)
        {
            var teacherId = GetTeacherId();
            var exam = await _db.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == id && e.CreatedByTeacherId == teacherId);

            if (exam == null) return NotFound();

            if (!exam.IsPublished && exam.Questions.Count == 0)
            {
                TempData["Error"] = "Đề thi phải có ít nhất 1 câu hỏi trước khi công bố.";
                return RedirectToAction("EditExam", new { id });
            }

            exam.IsPublished = !exam.IsPublished;
            exam.PublishedAt = exam.IsPublished ? DateTime.Now : null;
            exam.UpdatedAt = DateTime.Now;

            await _db.SaveChangesAsync();

            TempData["Success"] = exam.IsPublished
                ? "Đề thi đã được công bố cho sinh viên."
                : "Đã ẩn đề thi.";

            return RedirectToAction("EditExam", new { id });
        }

        // ============================================
        // ADD QUESTION
        // ============================================
        [HttpGet]
        public async Task<IActionResult> AddQuestion(int examId)
        {
            var teacherId = GetTeacherId();
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId && e.CreatedByTeacherId == teacherId);
            if (exam == null) return NotFound();

            var vm = new AddQuestionViewModel
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                Answers = new List<AnswerInputViewModel>
                {
                    new(), new(), new(), new()
                }
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(AddQuestionViewModel model, string? trueFalseCorrect)
        {
            var teacherId = GetTeacherId();
            var exam = await _db.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == model.ExamId && e.CreatedByTeacherId == teacherId);

            if (exam == null) return NotFound();

            // Xử lý TrueFalse: tự tạo 2 đáp án Đúng/Sai
            if (model.QuestionType == "TrueFalse")
            {
                bool correctIsTrue = trueFalseCorrect != "false";
                model.Answers = new List<AnswerInputViewModel>
                {
                    new() { AnswerText = "Đúng", IsCorrect = correctIsTrue },
                    new() { AnswerText = "Sai",  IsCorrect = !correctIsTrue }
                };
            }

            // Xóa lỗi validation của Answers (Essay không cần đáp án)
            foreach (var key in ModelState.Keys.Where(k => k.StartsWith("Answers")).ToList())
                ModelState.Remove(key);

            if (!ModelState.IsValid) return View(model);

            // Validate answers for MultipleChoice only
            if (model.QuestionType == "MultipleChoice")
            {
                var validAnswers = model.Answers.Where(a => !string.IsNullOrWhiteSpace(a.AnswerText)).ToList();
                if (validAnswers.Count < 2)
                {
                    ModelState.AddModelError("", "Câu hỏi trắc nghiệm phải có ít nhất 2 đáp án.");
                    return View(model);
                }
                if (!validAnswers.Any(a => a.IsCorrect))
                {
                    ModelState.AddModelError("", "Phải chọn ít nhất 1 đáp án đúng.");
                    return View(model);
                }
            }

            var question = new Question
            {
                ExamId = model.ExamId,
                QuestionText = model.QuestionText,
                QuestionType = model.QuestionType,
                Points = model.Points,
                Explanation = model.Explanation,
                OrderIndex = exam.Questions.Count + 1
            };

            _db.Questions.Add(question);
            await _db.SaveChangesAsync();

            if (model.QuestionType != "Essay")
            {
                var answers = model.Answers
                    .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                    .Select((a, i) => new Answer
                    {
                        QuestionId = question.QuestionId,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect,
                        OrderIndex = i
                    }).ToList();

                _db.Answers.AddRange(answers);
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Thêm câu hỏi thành công!";
            return RedirectToAction("EditExam", new { id = model.ExamId });
        }

        // ============================================
        // EDIT QUESTION
        // ============================================
        [HttpGet]
        public async Task<IActionResult> EditQuestion(int id)
        {
            var teacherId = GetTeacherId();
            var question = await _db.Questions
                .Include(q => q.Exam)
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == id && q.Exam.CreatedByTeacherId == teacherId);

            if (question == null) return NotFound();

            var vm = new AddQuestionViewModel
            {
                ExamId = question.ExamId,
                ExamTitle = question.Exam.Title,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Points = question.Points,
                Explanation = question.Explanation,
                Answers = question.Answers.OrderBy(a => a.OrderIndex)
                    .Select(a => new AnswerInputViewModel
                    {
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect
                    }).ToList()
            };

            // Ensure at least 4 answer slots
            while (vm.Answers.Count < 4)
                vm.Answers.Add(new AnswerInputViewModel());

            ViewBag.QuestionId = id;
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditQuestion(int id, AddQuestionViewModel model)
        {
            var teacherId = GetTeacherId();
            var question = await _db.Questions
                .Include(q => q.Exam)
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.QuestionId == id && q.Exam.CreatedByTeacherId == teacherId);

            if (question == null) return NotFound();
            if (!ModelState.IsValid) { ViewBag.QuestionId = id; return View(model); }

            question.QuestionText = model.QuestionText;
            question.QuestionType = model.QuestionType;
            question.Points = model.Points;
            question.Explanation = model.Explanation;

            // Update answers
            _db.Answers.RemoveRange(question.Answers);
            await _db.SaveChangesAsync();

            if (model.QuestionType != "Essay")
            {
                var newAnswers = model.Answers
                    .Where(a => !string.IsNullOrWhiteSpace(a.AnswerText))
                    .Select((a, i) => new Answer
                    {
                        QuestionId = id,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect,
                        OrderIndex = i
                    }).ToList();
                _db.Answers.AddRange(newAnswers);
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Cập nhật câu hỏi thành công!";
            return RedirectToAction("EditExam", new { id = question.ExamId });
        }

        // ============================================
        // DELETE QUESTION
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuestion(int id)
        {
            var teacherId = GetTeacherId();
            var question = await _db.Questions
                .Include(q => q.Exam)
                .FirstOrDefaultAsync(q => q.QuestionId == id && q.Exam.CreatedByTeacherId == teacherId);

            if (question == null) return NotFound();

            var examId = question.ExamId;
            _db.Questions.Remove(question);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xóa câu hỏi.";
            return RedirectToAction("EditExam", new { id = examId });
        }

        // ============================================
        // GRADING
        // ============================================
        public async Task<IActionResult> Submissions(int examId)
        {
            var teacherId = GetTeacherId();
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId && e.CreatedByTeacherId == teacherId);
            if (exam == null) return NotFound();

            var submissions = await _db.ExamSubmissions
                .Include(es => es.Student).ThenInclude(s => s.User)
                .Where(es => es.ExamId == examId)
                .OrderByDescending(es => es.SubmittedAt)
                .ToListAsync();

            ViewBag.Exam = exam;
            return View(submissions);
        }

        [HttpGet]
        public async Task<IActionResult> GradeSubmission(int id)
        {
            var teacherId = GetTeacherId();
            var submission = await _db.ExamSubmissions
                .Include(es => es.Exam)
                .Include(es => es.Student).ThenInclude(s => s.User)
                .Include(es => es.StudentAnswers)
                    .ThenInclude(sa => sa.Question)
                .FirstOrDefaultAsync(es => es.SubmissionId == id && es.Exam.CreatedByTeacherId == teacherId);

            if (submission == null) return NotFound();

            var essayAnswers = submission.StudentAnswers
                .Where(sa => sa.Question.QuestionType == "Essay")
                .Select(sa => new EssayGradeViewModel
                {
                    StudentAnswerId = sa.StudentAnswerId,
                    QuestionId = sa.QuestionId,
                    QuestionText = sa.Question.QuestionText,
                    Points = sa.Question.Points,
                    EssayAnswer = sa.EssayAnswer,
                    ScoreEarned = sa.ScoreEarned
                }).ToList();

            var autoScore = submission.StudentAnswers
                .Where(sa => sa.Question.QuestionType != "Essay" && sa.ScoreEarned.HasValue)
                .Sum(sa => sa.ScoreEarned ?? 0);

            var vm = new GradeSubmissionViewModel
            {
                SubmissionId = submission.SubmissionId,
                ExamId = submission.ExamId,
                ExamTitle = submission.Exam.Title,
                StudentName = submission.Student.User.FullName,
                StudentCode = submission.Student.StudentCode,
                AutoScore = autoScore,
                MaxScore = submission.Exam.MaxScore,
                TeacherComment = submission.TeacherComment,
                EssayQuestions = essayAnswers
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GradeSubmission(int id, GradeSubmissionViewModel model)
        {
            var teacherId = GetTeacherId();
            var submission = await _db.ExamSubmissions
                .Include(es => es.Exam)
                .Include(es => es.StudentAnswers).ThenInclude(sa => sa.Question)
                .FirstOrDefaultAsync(es => es.SubmissionId == id && es.Exam.CreatedByTeacherId == teacherId);

            if (submission == null) return NotFound();

            var essayScores = new Dictionary<int, decimal>();
            foreach (var key in Request.Form.Keys.Where(k => k.StartsWith("essayScores[")))
            {
                var start = "essayScores[".Length;
                var end = key.IndexOf(']', start);
                if (end <= start) continue;

                var idPart = key.Substring(start, end - start);
                if (!int.TryParse(idPart, out var studentAnswerId)) continue;

                var value = Request.Form[key].ToString();
                if (decimal.TryParse(value, out var score))
                    essayScores[studentAnswerId] = score;
            }

            // Grade essay questions
            foreach (var sa in submission.StudentAnswers.Where(sa => sa.Question.QuestionType == "Essay"))
            {
                if (essayScores.TryGetValue(sa.StudentAnswerId, out var score))
                {
                    sa.ScoreEarned = Math.Min(score, sa.Question.Points);
                    sa.GradedAt = DateTime.Now;
                }
            }

            // Calculate total score
            var totalRaw = submission.StudentAnswers.Sum(sa => sa.ScoreEarned ?? 0);
            var maxRaw = submission.StudentAnswers.Sum(sa => sa.Question.Points);
            submission.TotalScore = maxRaw > 0
                ? Math.Round(totalRaw / maxRaw * submission.Exam.MaxScore, 2)
                : 0;

            submission.TeacherComment = model.TeacherComment;
            submission.Status = "Graded";

            await _db.SaveChangesAsync();
            TempData["Success"] = "Chấm bài thành công!";
            return RedirectToAction("Submissions", new { examId = submission.ExamId });
        }

        // ============================================
        // STATISTICS
        // ============================================
        public async Task<IActionResult> Statistics(int examId)
        {
            var teacherId = GetTeacherId();
            var exam = await _db.Exams.FirstOrDefaultAsync(e => e.ExamId == examId && e.CreatedByTeacherId == teacherId);
            if (exam == null) return NotFound();

            var submissions = await _db.ExamSubmissions
                .Include(es => es.Student).ThenInclude(s => s.User)
                .Where(es => es.ExamId == examId && es.Status != "InProgress")
                .ToListAsync();

            var vm = new ClassStatisticsViewModel
            {
                ExamId = examId,
                ExamTitle = exam.Title,
                TotalStudents = submissions.Count,
                SubmittedCount = submissions.Count(s => s.Status != "InProgress"),
                AverageScore = submissions.Any(s => s.TotalScore.HasValue)
                    ? Math.Round(submissions.Where(s => s.TotalScore.HasValue).Average(s => s.TotalScore!.Value), 2)
                    : null,
                HighestScore = submissions.Max(s => s.TotalScore),
                LowestScore = submissions.Where(s => s.TotalScore.HasValue).Min(s => s.TotalScore),
                StudentResults = submissions.Select(s => new StudentResultSummary
                {
                    StudentName = s.Student.User.FullName,
                    StudentCode = s.Student.StudentCode,
                    ClassName = s.Student.ClassName,
                    TotalScore = s.TotalScore,
                    Status = s.Status,
                    SubmittedAt = s.SubmittedAt
                }).OrderByDescending(s => s.TotalScore).ToList()
            };

            return View(vm);
        }
    }
}
