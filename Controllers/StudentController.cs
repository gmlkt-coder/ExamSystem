using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ExamSystem.Data;
using ExamSystem.Models;
using ExamSystem.ViewModels;

namespace ExamSystem.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly ExamDbContext _db;

        public StudentController(ExamDbContext db)
        {
            _db = db;
        }

        private int GetStudentId() => int.Parse(User.FindFirst("StudentId")!.Value);

        // ============================================
        // DASHBOARD
        // ============================================
        public async Task<IActionResult> Index()
        {
            var studentId = GetStudentId();
            var student = await _db.Students.Include(s => s.User).FirstAsync(s => s.StudentId == studentId);

            var submissions = await _db.ExamSubmissions
                .Include(es => es.Exam)
                .Where(es => es.StudentId == studentId && es.Status != "InProgress")
                .OrderByDescending(es => es.SubmittedAt)
                .Take(5)
                .ToListAsync();

            var availableExams = await _db.Exams
                .Include(e => e.Questions)
                .Include(e => e.Teacher).ThenInclude(t => t.User)
                .Where(e => e.IsPublished && e.ExamType == "Exam"
                    && !_db.ExamSubmissions.Any(es => es.ExamId == e.ExamId && es.StudentId == studentId))
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
                    TeacherName = e.Teacher.User.FullName
                })
                .ToListAsync();

            var gradedSubmissions = submissions.Where(s => s.TotalScore.HasValue).ToList();

            var vm = new StudentDashboardViewModel
            {
                StudentName = student.User.FullName,
                StudentCode = student.StudentCode,
                AverageScore = gradedSubmissions.Any() ? Math.Round(gradedSubmissions.Average(s => s.TotalScore!.Value), 2) : null,
                TotalExamsTaken = await _db.ExamSubmissions.CountAsync(es => es.StudentId == studentId && es.Status != "InProgress"),
                RecentSubmissions = submissions,
                AvailableExams = availableExams
            };

            return View(vm);
        }

        // ============================================
        // EXAM LIST
        // ============================================
        public async Task<IActionResult> Exams()
        {
            var studentId = GetStudentId();
            var exams = await _db.Exams
                .Include(e => e.Questions)
                .Include(e => e.Teacher).ThenInclude(t => t.User)
                .Where(e => e.IsPublished && e.ExamType == "Exam")
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
                    TeacherName = e.Teacher.User.FullName
                })
                .ToListAsync();

            // Mark already attempted exams
            var mySubmissions = await _db.ExamSubmissions
                .Where(es => es.StudentId == studentId)
                .Select(es => es.ExamId)
                .ToListAsync();

            ViewBag.MySubmissions = mySubmissions;
            return View(exams);
        }

        // ============================================
        // EXAM HISTORY
        // ============================================
        public async Task<IActionResult> History()
        {
            var studentId = GetStudentId();
            var submissions = await _db.ExamSubmissions
                .Include(es => es.Exam)
                .Where(es => es.StudentId == studentId)
                .OrderByDescending(es => es.StartedAt)
                .ToListAsync();

            return View(submissions);
        }

        // ============================================
        // PRACTICE EXAMS
        // ============================================
        public async Task<IActionResult> Practice()
        {
            var studentId = GetStudentId();
            var practices = await _db.PracticeExams
                .Include(pe => pe.Questions)
                .Include(pe => pe.Submissions)
                .Where(pe => pe.StudentId == studentId)
                .OrderByDescending(pe => pe.CreatedAt)
                .ToListAsync();

            return View(practices);
        }

        [HttpGet]
        public IActionResult CreatePractice()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePractice(CreatePracticeExamViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var practice = new PracticeExam
            {
                StudentId = GetStudentId(),
                Title = model.Title,
                Description = model.Description
            };

            _db.PracticeExams.Add(practice);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Tạo đề luyện tập thành công!";
            return RedirectToAction("EditPractice", new { id = practice.PracticeExamId });
        }

        [HttpGet]
        public async Task<IActionResult> EditPractice(int id)
        {
            var studentId = GetStudentId();
            var practice = await _db.PracticeExams
                .Include(pe => pe.Questions).ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(pe => pe.PracticeExamId == id && pe.StudentId == studentId);

            if (practice == null) return NotFound();
            return View(practice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPracticeQuestion(int practiceExamId, string questionText,
            string questionType, decimal points, string explanation,
            List<string> answerTexts, List<bool> isCorrect)
        {
            var studentId = GetStudentId();
            var practice = await _db.PracticeExams
                .Include(pe => pe.Questions)
                .FirstOrDefaultAsync(pe => pe.PracticeExamId == practiceExamId && pe.StudentId == studentId);

            if (practice == null) return NotFound();

            var question = new PracticeQuestion
            {
                PracticeExamId = practiceExamId,
                QuestionText = questionText,
                QuestionType = questionType,
                Points = points,
                Explanation = explanation,
                OrderIndex = practice.Questions.Count + 1
            };

            _db.PracticeQuestions.Add(question);
            await _db.SaveChangesAsync();

            if (questionType != "Essay")
            {
                for (int i = 0; i < answerTexts.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(answerTexts[i]))
                    {
                        _db.PracticeAnswers.Add(new PracticeAnswer
                        {
                            PracticeQuestionId = question.PracticeQuestionId,
                            AnswerText = answerTexts[i],
                            IsCorrect = i < isCorrect.Count && isCorrect[i],
                            OrderIndex = i
                        });
                    }
                }
                await _db.SaveChangesAsync();
            }

            TempData["Success"] = "Thêm câu hỏi thành công!";
            return RedirectToAction("EditPractice", new { id = practiceExamId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePracticeQuestion(int id, string questionText, decimal points,
            string? explanation, List<string>? answerTexts, List<bool>? isCorrect)
        {
            var studentId = GetStudentId();
            var question = await _db.PracticeQuestions
                .Include(q => q.PracticeExam)
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.PracticeQuestionId == id && q.PracticeExam.StudentId == studentId);

            if (question == null) return NotFound();

            question.QuestionText = questionText;
            question.Points = points;
            question.Explanation = explanation;

            if (question.QuestionType == "MultipleChoice" && answerTexts != null)
            {
                _db.PracticeAnswers.RemoveRange(question.Answers);
                await _db.SaveChangesAsync();
                for (int i = 0; i < answerTexts.Count; i++)
                {
                    if (!string.IsNullOrWhiteSpace(answerTexts[i]))
                    {
                        _db.PracticeAnswers.Add(new PracticeAnswer
                        {
                            PracticeQuestionId = id,
                            AnswerText = answerTexts[i],
                            IsCorrect = isCorrect != null && i < isCorrect.Count && isCorrect[i],
                            OrderIndex = i
                        });
                    }
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật câu hỏi.";
            return RedirectToAction("EditPractice", new { id = question.PracticeExamId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePracticeQuestion(int id)
        {
            var studentId = GetStudentId();
            var question = await _db.PracticeQuestions
                .Include(q => q.PracticeExam)
                .FirstOrDefaultAsync(q => q.PracticeQuestionId == id && q.PracticeExam.StudentId == studentId);

            if (question == null) return NotFound();

            var practiceId = question.PracticeExamId;
            _db.PracticeQuestions.Remove(question);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Đã xóa câu hỏi.";
            return RedirectToAction("EditPractice", new { id = practiceId });
        }
    }

    // ============================================
    // EXAM CONTROLLER (Shared exam-taking logic)
    // ============================================
    [Authorize(Roles = "Student")]
    public class ExamController : Controller
    {
        private readonly ExamDbContext _db;

        public ExamController(ExamDbContext db)
        {
            _db = db;
        }

        private int GetStudentId() => int.Parse(User.FindFirst("StudentId")!.Value);

        // ============================================
        // ENTER EXAM (Password check)
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Enter(int id)
        {
            var studentId = GetStudentId();
            var exam = await _db.Exams
                .Include(e => e.Questions)
                .FirstOrDefaultAsync(e => e.ExamId == id && e.IsPublished);

            if (exam == null) return NotFound();

            // Check if already submitted
            var existing = await _db.ExamSubmissions
                .FirstOrDefaultAsync(es => es.ExamId == id && es.StudentId == studentId);

            if (existing != null && existing.Status != "InProgress")
            {
                TempData["Info"] = "Bạn đã nộp bài thi này rồi.";
                return RedirectToAction("Result", new { id = existing.SubmissionId });
            }

            // If in progress, resume
            if (existing?.Status == "InProgress")
                return RedirectToAction("Take", new { submissionId = existing.SubmissionId });

            // Check time window
            if (exam.StartTime.HasValue && DateTime.Now < exam.StartTime.Value)
            {
                TempData["Error"] = $"Bài thi chưa bắt đầu. Bắt đầu lúc {exam.StartTime:dd/MM/yyyy HH:mm}";
                return RedirectToAction("Exams", "Student");
            }

            if (exam.EndTime.HasValue && DateTime.Now > exam.EndTime.Value)
            {
                TempData["Error"] = "Bài thi đã kết thúc.";
                return RedirectToAction("Exams", "Student");
            }

            var vm = new ExamPasswordViewModel
            {
                ExamId = exam.ExamId,
                ExamTitle = exam.Title,
                Subject = exam.Subject,
                Duration = exam.Duration,
                QuestionCount = exam.Questions.Count
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Enter(ExamPasswordViewModel model)
        {
            var studentId = GetStudentId();
            var exam = await _db.Exams
                .Include(e => e.Questions).ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(e => e.ExamId == model.ExamId && e.IsPublished);

            if (exam == null) return NotFound();

            // Verify password
            if (!string.IsNullOrEmpty(exam.Password) && exam.Password != model.Password)
            {
                ModelState.AddModelError("Password", "Mật khẩu bài thi không đúng.");
                model.ExamTitle = exam.Title;
                model.Duration = exam.Duration;
                model.QuestionCount = exam.Questions.Count;
                return View(model);
            }

            // Create submission
            var submission = new ExamSubmission
            {
                ExamId = exam.ExamId,
                StudentId = studentId,
                StartedAt = DateTime.Now,
                Status = "InProgress"
            };
            _db.ExamSubmissions.Add(submission);
            await _db.SaveChangesAsync();

            // Create blank student answers
            var studentAnswers = exam.Questions.Select(q => new StudentAnswer
            {
                SubmissionId = submission.SubmissionId,
                QuestionId = q.QuestionId
            }).ToList();
            _db.StudentAnswers.AddRange(studentAnswers);
            await _db.SaveChangesAsync();

            return RedirectToAction("Take", new { submissionId = submission.SubmissionId });
        }

        // ============================================
        // TAKE EXAM
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Take(int submissionId)
        {
            var studentId = GetStudentId();
            var submission = await _db.ExamSubmissions
                .Include(es => es.Exam).ThenInclude(e => e.Questions).ThenInclude(q => q.Answers)
                .Include(es => es.StudentAnswers)
                .FirstOrDefaultAsync(es => es.SubmissionId == submissionId && es.StudentId == studentId);

            if (submission == null || submission.Status != "InProgress") return NotFound();

            // Check if time expired
            var elapsed = (DateTime.Now - submission.StartedAt).TotalMinutes;
            if (elapsed > submission.Exam.Duration)
            {
                await AutoSubmit(submissionId);
                return RedirectToAction("Result", new { id = submissionId });
            }

            var vm = new TakeExamViewModel
            {
                ExamId = submission.ExamId,
                SubmissionId = submissionId,
                ExamTitle = submission.Exam.Title,
                Subject = submission.Exam.Subject,
                Duration = submission.Exam.Duration,
                StartedAt = submission.StartedAt,
                Questions = submission.Exam.Questions
                    .OrderBy(q => q.OrderIndex)
                    .Select(q => new QuestionViewModel
                    {
                        QuestionId = q.QuestionId,
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        Points = q.Points,
                        OrderIndex = q.OrderIndex,
                        Answers = q.Answers.OrderBy(a => a.OrderIndex)
                            .Select(a => new AnswerViewModel
                            {
                                AnswerId = a.AnswerId,
                                AnswerText = a.AnswerText
                            }).ToList(),
                        SelectedAnswerId = submission.StudentAnswers
                            .FirstOrDefault(sa => sa.QuestionId == q.QuestionId)?.SelectedAnswerId,
                        EssayAnswer = submission.StudentAnswers
                            .FirstOrDefault(sa => sa.QuestionId == q.QuestionId)?.EssayAnswer
                    }).ToList()
            };

            return View(vm);
        }

        // ============================================
        // SAVE ANSWER (Auto-save via AJAX)
        // ============================================
        [HttpPost]
        public async Task<IActionResult> SaveAnswer(int submissionId, int questionId,
            int? selectedAnswerId, string? essayAnswer)
        {
            var studentId = GetStudentId();
            var submission = await _db.ExamSubmissions
                .FirstOrDefaultAsync(es => es.SubmissionId == submissionId && es.StudentId == studentId);

            if (submission == null || submission.Status != "InProgress")
                return Json(new { success = false });

            var sa = await _db.StudentAnswers
                .FirstOrDefaultAsync(sa => sa.SubmissionId == submissionId && sa.QuestionId == questionId);

            if (sa == null) return Json(new { success = false });

            sa.SelectedAnswerId = selectedAnswerId;
            sa.EssayAnswer = essayAnswer;
            sa.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // ============================================
        // SUBMIT EXAM
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int submissionId)
        {
            var studentId = GetStudentId();
            var submission = await _db.ExamSubmissions
                .Include(es => es.Exam).ThenInclude(e => e.Questions).ThenInclude(q => q.Answers)
                .Include(es => es.StudentAnswers)
                .FirstOrDefaultAsync(es => es.SubmissionId == submissionId && es.StudentId == studentId);

            if (submission == null || submission.Status != "InProgress") return NotFound();

            await GradeObjectiveQuestions(submission);

            submission.SubmittedAt = DateTime.Now;
            submission.Status = "Submitted";

            // If no essay questions, set to Graded
            bool hasEssay = submission.Exam.Questions.Any(q => q.QuestionType == "Essay");
            if (!hasEssay)
            {
                submission.Status = "Graded";
                var totalRaw = submission.StudentAnswers.Sum(sa => sa.ScoreEarned ?? 0);
                var maxRaw = submission.Exam.Questions.Sum(q => q.Points);
                submission.TotalScore = maxRaw > 0
                    ? Math.Round(totalRaw / maxRaw * submission.Exam.MaxScore, 2)
                    : 0;
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Nộp bài thành công!";
            return RedirectToAction("Result", new { id = submissionId });
        }

        private async Task AutoSubmit(int submissionId)
        {
            var submission = await _db.ExamSubmissions
                .Include(es => es.Exam).ThenInclude(e => e.Questions).ThenInclude(q => q.Answers)
                .Include(es => es.StudentAnswers)
                .FirstOrDefaultAsync(es => es.SubmissionId == submissionId);

            if (submission == null || submission.Status != "InProgress") return;

            await GradeObjectiveQuestions(submission);
            submission.SubmittedAt = DateTime.Now;
            submission.IsAutoSubmit = true;

            bool hasEssay = submission.Exam.Questions.Any(q => q.QuestionType == "Essay");
            if (!hasEssay)
            {
                submission.Status = "Graded";
                var totalRaw = submission.StudentAnswers.Sum(sa => sa.ScoreEarned ?? 0);
                var maxRaw = submission.Exam.Questions.Sum(q => q.Points);
                submission.TotalScore = maxRaw > 0
                    ? Math.Round(totalRaw / maxRaw * submission.Exam.MaxScore, 2)
                    : 0;
            }
            else
            {
                submission.Status = "Submitted";
            }

            await _db.SaveChangesAsync();
        }

        private async Task GradeObjectiveQuestions(ExamSubmission submission)
        {
            foreach (var sa in submission.StudentAnswers)
            {
                var question = submission.Exam.Questions.FirstOrDefault(q => q.QuestionId == sa.QuestionId);
                if (question == null) continue;

                if (question.QuestionType == "MultipleChoice" || question.QuestionType == "TrueFalse")
                {
                    bool hasCorrectAnswer = question.Answers.Any(a => a.IsCorrect);

                    if (!hasCorrectAnswer)
                    {
                        // Câu hỏi không có đáp án đúng -> không tính điểm, không tính sai
                        sa.IsCorrect = null;
                        sa.ScoreEarned = 0;
                    }
                    else if (sa.SelectedAnswerId.HasValue)
                    {
                        var answer = question.Answers.FirstOrDefault(a => a.AnswerId == sa.SelectedAnswerId);
                        sa.IsCorrect = answer?.IsCorrect ?? false;
                        sa.ScoreEarned = sa.IsCorrect == true ? question.Points : 0;
                    }
                    else
                    {
                        // Không chọn đáp án -> sai (chỉ khi câu hỏi có đáp án đúng)
                        sa.IsCorrect = false;
                        sa.ScoreEarned = 0;
                    }
                    sa.GradedAt = DateTime.Now;
                }
            }
            await _db.SaveChangesAsync();
        }

        // ============================================
        // RESULT
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Result(int id)
        {
            var studentId = GetStudentId();
            var submission = await _db.ExamSubmissions
                .Include(es => es.Exam).ThenInclude(e => e.Questions).ThenInclude(q => q.Answers)
                .Include(es => es.StudentAnswers).ThenInclude(sa => sa.SelectedAnswer)
                .Include(es => es.StudentAnswers).ThenInclude(sa => sa.Question)
                .FirstOrDefaultAsync(es => es.SubmissionId == id && es.StudentId == studentId);

            if (submission == null) return NotFound();

            var vm = new ExamResultViewModel
            {
                SubmissionId = id,
                ExamId = submission.ExamId,
                ExamTitle = submission.Exam.Title,
                Subject = submission.Exam.Subject,
                TotalScore = submission.TotalScore,
                MaxScore = submission.Exam.MaxScore,
                Status = submission.Status,
                SubmittedAt = submission.SubmittedAt,
                TeacherComment = submission.TeacherComment,
                AllowReview = submission.Exam.AllowReview,
                CorrectCount = submission.StudentAnswers.Count(sa => sa.IsCorrect == true),
                TotalQuestions = submission.Exam.Questions.Count,
                Questions = submission.Status == "Graded" && submission.Exam.AllowReview
                    ? submission.Exam.Questions.OrderBy(q => q.OrderIndex).Select(q =>
                    {
                        var sa = submission.StudentAnswers.FirstOrDefault(a => a.QuestionId == q.QuestionId);
                        return new QuestionResultViewModel
                        {
                            QuestionId = q.QuestionId,
                            QuestionText = q.QuestionText,
                            QuestionType = q.QuestionType,
                            Points = q.Points,
                            ScoreEarned = sa?.ScoreEarned,
                            IsCorrect = sa?.IsCorrect,
                            EssayAnswer = sa?.EssayAnswer,
                            Explanation = q.Explanation,
                            SelectedAnswerId = sa?.SelectedAnswerId,
                            Answers = q.Answers.OrderBy(a => a.OrderIndex).Select(a => new AnswerResultViewModel
                            {
                                AnswerId = a.AnswerId,
                                AnswerText = a.AnswerText,
                                IsCorrect = a.IsCorrect
                            }).ToList()
                        };
                    }).ToList()
                    : new()
            };

            return View(vm);
        }

        // ============================================
        // TAKE PRACTICE EXAM
        // ============================================
        [HttpGet]
        public async Task<IActionResult> TakePractice(int id)
        {
            var studentId = GetStudentId();
            var practice = await _db.PracticeExams
                .Include(pe => pe.Questions).ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(pe => pe.PracticeExamId == id && pe.StudentId == studentId);

            if (practice == null) return NotFound();

            return View(practice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPractice(int practiceExamId,
            [FromForm] IFormCollection form)
        {
            var studentId = GetStudentId();
            var practice = await _db.PracticeExams
                .Include(pe => pe.Questions).ThenInclude(q => q.Answers)
                .FirstOrDefaultAsync(pe => pe.PracticeExamId == practiceExamId && pe.StudentId == studentId);

            if (practice == null) return NotFound();

            var selectedAnswers = new Dictionary<int, int>();
            var essayAnswers = new Dictionary<int, string>();

            foreach (var key in form.Keys)
            {
                if (key.StartsWith("selectedAnswers["))
                {
                    var start = "selectedAnswers[".Length;
                    var end = key.IndexOf(']', start);
                    if (end <= start) continue;
                    var idPart = key.Substring(start, end - start);
                    if (!int.TryParse(idPart, out var questionId)) continue;
                    if (int.TryParse(form[key].ToString(), out var answerId))
                        selectedAnswers[questionId] = answerId;
                }
                else if (key.StartsWith("essayAnswers["))
                {
                    var start = "essayAnswers[".Length;
                    var end = key.IndexOf(']', start);
                    if (end <= start) continue;
                    var idPart = key.Substring(start, end - start);
                    if (!int.TryParse(idPart, out var questionId)) continue;
                    essayAnswers[questionId] = form[key].ToString();
                }
            }

            decimal totalScore = 0;
            decimal maxScore = practice.Questions.Sum(q => q.Points);

            foreach (var q in practice.Questions)
            {
                if (q.QuestionType == "Essay") continue; // Essay không tự chấm
                // Chỉ tính sai nếu câu hỏi CÓ đáp án đúng và học sinh KHÔNG chọn đúng
                bool hasCorrectAnswer = q.Answers.Any(a => a.IsCorrect);
                if (!hasCorrectAnswer) continue; // Không có đáp án đúng -> không tính
                if (selectedAnswers.TryGetValue(q.PracticeQuestionId, out var answerId))
                {
                    var answer = q.Answers.FirstOrDefault(a => a.PracticeAnswerId == answerId);
                    if (answer?.IsCorrect == true)
                        totalScore += q.Points;
                }
            }

            var submission = new PracticeSubmission
            {
                PracticeExamId = practiceExamId,
                StudentId = studentId,
                TotalScore = totalScore,
                MaxScore = maxScore
            };
            _db.PracticeSubmissions.Add(submission);
            await _db.SaveChangesAsync();

            ViewBag.Score = totalScore;
            ViewBag.MaxScore = maxScore;
            ViewBag.SelectedAnswers = selectedAnswers;
            ViewBag.EssayAnswers = essayAnswers;

            return View("PracticeResult", practice);
        }
    }
}
