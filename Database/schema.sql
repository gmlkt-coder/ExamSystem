-- ============================================
-- HỆ THỐNG QUẢN LÝ VÀ LÀM BÀI THI
-- SQL Server Database Schema
-- ============================================

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = 'ExamSystemDB')
    DROP DATABASE ExamSystemDB;
GO

CREATE DATABASE ExamSystemDB;
GO

USE ExamSystemDB;
GO

-- ============================================
-- BẢNG USERS (Người dùng)
-- ============================================
CREATE TABLE Users (
    UserId      INT IDENTITY(1,1) PRIMARY KEY,
    Username    NVARCHAR(50) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,
    FullName    NVARCHAR(100) NOT NULL,
    Email       NVARCHAR(100),
    Phone       NVARCHAR(20),
    Role        NVARCHAR(20) NOT NULL CHECK (Role IN ('Admin', 'Teacher', 'Student')),
    IsLocked    BIT NOT NULL DEFAULT 0,
    CreatedAt   DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt   DATETIME NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- BẢNG STUDENTS (Thông tin sinh viên)
-- ============================================
CREATE TABLE Students (
    StudentId   INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT NOT NULL UNIQUE REFERENCES Users(UserId) ON DELETE CASCADE,
    StudentCode NVARCHAR(20) NOT NULL UNIQUE,
    ClassName   NVARCHAR(50),
    DateOfBirth DATE
);

-- ============================================
-- BẢNG TEACHERS (Thông tin giáo viên)
-- ============================================
CREATE TABLE Teachers (
    TeacherId   INT IDENTITY(1,1) PRIMARY KEY,
    UserId      INT NOT NULL UNIQUE REFERENCES Users(UserId) ON DELETE CASCADE,
    TeacherCode NVARCHAR(20) NOT NULL UNIQUE,
    Department  NVARCHAR(100),
    Degree      NVARCHAR(50)
);

-- ============================================
-- BẢNG EXAMS (Đề thi)
-- ============================================
CREATE TABLE Exams (
    ExamId          INT IDENTITY(1,1) PRIMARY KEY,
    Title           NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(MAX),
    Subject         NVARCHAR(100),
    Duration        INT NOT NULL,  -- phút
    Password        NVARCHAR(100),
    MaxScore        DECIMAL(5,2) NOT NULL DEFAULT 10.0,
    IsPublished     BIT NOT NULL DEFAULT 0,
    PublishedAt     DATETIME,
    StartTime       DATETIME,
    EndTime         DATETIME,
    AllowReview     BIT NOT NULL DEFAULT 0,
    CreatedByTeacherId INT NOT NULL REFERENCES Teachers(TeacherId),
    ExamType        NVARCHAR(20) NOT NULL DEFAULT 'Exam' CHECK (ExamType IN ('Exam', 'Practice')),
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- BẢNG QUESTIONS (Câu hỏi)
-- ============================================
CREATE TABLE Questions (
    QuestionId      INT IDENTITY(1,1) PRIMARY KEY,
    ExamId          INT NOT NULL REFERENCES Exams(ExamId) ON DELETE CASCADE,
    QuestionText    NVARCHAR(MAX) NOT NULL,
    QuestionType    NVARCHAR(20) NOT NULL CHECK (QuestionType IN ('MultipleChoice', 'TrueFalse', 'Essay')),
    Points          DECIMAL(5,2) NOT NULL DEFAULT 1.0,
    OrderIndex      INT NOT NULL DEFAULT 0,
    Explanation     NVARCHAR(MAX),
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- BẢNG ANSWERS (Đáp án câu hỏi trắc nghiệm)
-- ============================================
CREATE TABLE Answers (
    AnswerId        INT IDENTITY(1,1) PRIMARY KEY,
    QuestionId      INT NOT NULL REFERENCES Questions(QuestionId) ON DELETE CASCADE,
    AnswerText      NVARCHAR(MAX) NOT NULL,
    IsCorrect       BIT NOT NULL DEFAULT 0,
    OrderIndex      INT NOT NULL DEFAULT 0
);

-- ============================================
-- BẢNG EXAM_SUBMISSIONS (Bài nộp)
-- ============================================
CREATE TABLE ExamSubmissions (
    SubmissionId    INT IDENTITY(1,1) PRIMARY KEY,
    ExamId          INT NOT NULL REFERENCES Exams(ExamId),
    StudentId       INT NOT NULL REFERENCES Students(StudentId),
    StartedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    SubmittedAt     DATETIME,
    IsAutoSubmit    BIT NOT NULL DEFAULT 0,
    TotalScore      DECIMAL(5,2),
    Status          NVARCHAR(20) NOT NULL DEFAULT 'InProgress' 
                    CHECK (Status IN ('InProgress', 'Submitted', 'Graded')),
    TeacherComment  NVARCHAR(MAX),
    UNIQUE(ExamId, StudentId)
);

-- ============================================
-- BẢNG STUDENT_ANSWERS (Câu trả lời của học sinh)
-- ============================================
CREATE TABLE StudentAnswers (
    StudentAnswerId INT IDENTITY(1,1) PRIMARY KEY,
    SubmissionId    INT NOT NULL REFERENCES ExamSubmissions(SubmissionId) ON DELETE CASCADE,
    QuestionId      INT NOT NULL REFERENCES Questions(QuestionId),
    SelectedAnswerId INT REFERENCES Answers(AnswerId),
    EssayAnswer     NVARCHAR(MAX),
    IsCorrect       BIT,
    ScoreEarned     DECIMAL(5,2),
    GradedAt        DATETIME,
    UpdatedAt       DATETIME NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- BẢNG PRACTICE_EXAMS (Đề luyện tập của Student)
-- ============================================
CREATE TABLE PracticeExams (
    PracticeExamId  INT IDENTITY(1,1) PRIMARY KEY,
    StudentId       INT NOT NULL REFERENCES Students(StudentId) ON DELETE CASCADE,
    Title           NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(MAX),
    CreatedAt       DATETIME NOT NULL DEFAULT GETDATE(),
    UpdatedAt       DATETIME NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- BẢNG PRACTICE_QUESTIONS (Câu hỏi luyện tập)
-- ============================================
CREATE TABLE PracticeQuestions (
    PracticeQuestionId INT IDENTITY(1,1) PRIMARY KEY,
    PracticeExamId  INT NOT NULL REFERENCES PracticeExams(PracticeExamId) ON DELETE CASCADE,
    QuestionText    NVARCHAR(MAX) NOT NULL,
    QuestionType    NVARCHAR(20) NOT NULL DEFAULT 'MultipleChoice',
    Points          DECIMAL(5,2) NOT NULL DEFAULT 1.0,
    OrderIndex      INT NOT NULL DEFAULT 0,
    CorrectAnswer   NVARCHAR(MAX),
    Explanation     NVARCHAR(MAX)
);

-- ============================================
-- BẢNG PRACTICE_ANSWERS (Đáp án luyện tập)
-- ============================================
CREATE TABLE PracticeAnswers (
    PracticeAnswerId INT IDENTITY(1,1) PRIMARY KEY,
    PracticeQuestionId INT NOT NULL REFERENCES PracticeQuestions(PracticeQuestionId) ON DELETE CASCADE,
    AnswerText      NVARCHAR(MAX) NOT NULL,
    IsCorrect       BIT NOT NULL DEFAULT 0,
    OrderIndex      INT NOT NULL DEFAULT 0
);

-- ============================================
-- BẢNG PRACTICE_SUBMISSIONS (Bài nộp luyện tập)
-- ============================================
CREATE TABLE PracticeSubmissions (
    PracticeSubmissionId INT IDENTITY(1,1) PRIMARY KEY,
    PracticeExamId  INT NOT NULL REFERENCES PracticeExams(PracticeExamId),
    StudentId       INT NOT NULL REFERENCES Students(StudentId),
    TotalScore      DECIMAL(5,2),
    MaxScore        DECIMAL(5,2),
    SubmittedAt     DATETIME NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- DEFAULT ADMIN ACCOUNT
-- Password: Admin@123 (BCrypt hashed)
-- ============================================
INSERT INTO Users (Username, PasswordHash, FullName, Email, Role)
VALUES (
    'admin',
    '$2a$11$rBnCO7LWWp7MZ/m1KI7hQOpnV8nHnqH8KxFHpKFvSU9Dqs5QBPDO6', -- Admin@123
    N'Quản trị viên hệ thống',
    'admin@examSystem.edu.vn',
    'Admin'
);

-- ============================================
-- SAMPLE TEACHER
-- ============================================
INSERT INTO Users (Username, PasswordHash, FullName, Email, Role)
VALUES (
    'GV101',
    '$2a$11$rBnCO7LWWp7MZ/m1KI7hQOpnV8nHnqH8KxFHpKFvSU9Dqs5QBPDO6', -- Admin@123
    N'Nguyễn Văn An',
    'gv101@examSystem.edu.vn',
    'Teacher'
);

INSERT INTO Teachers (UserId, TeacherCode, Department, Degree)
VALUES (2, 'GV101', N'Khoa Công nghệ thông tin', N'Thạc sĩ');

-- ============================================
-- SAMPLE STUDENT
-- ============================================
INSERT INTO Users (Username, PasswordHash, FullName, Email, Role)
VALUES (
    '20231234',
    '$2a$11$rBnCO7LWWp7MZ/m1KI7hQOpnV8nHnqH8KxFHpKFvSU9Dqs5QBPDO6', -- Admin@123
    N'Trần Thị Bình',
    '20231234@student.edu.vn',
    'Student'
);

INSERT INTO Students (UserId, StudentCode, ClassName, DateOfBirth)
VALUES (3, '20231234', 'CNTT-K65', '2005-03-15');

GO

-- ============================================
-- STORED PROCEDURES
-- ============================================

-- Lấy thống kê kết quả lớp cho Teacher
CREATE PROCEDURE sp_GetClassStatistics
    @ExamId INT,
    @TeacherId INT
AS
BEGIN
    SELECT 
        u.FullName AS StudentName,
        s.StudentCode,
        s.ClassName,
        es.TotalScore,
        es.Status,
        es.SubmittedAt,
        (SELECT COUNT(*) FROM StudentAnswers sa 
         JOIN Questions q ON sa.QuestionId = q.QuestionId
         WHERE sa.SubmissionId = es.SubmissionId AND sa.IsCorrect = 1) AS CorrectCount,
        (SELECT COUNT(*) FROM Questions q2 WHERE q2.ExamId = @ExamId) AS TotalQuestions
    FROM ExamSubmissions es
    JOIN Students s ON es.StudentId = s.StudentId
    JOIN Users u ON s.UserId = u.UserId
    WHERE es.ExamId = @ExamId
    ORDER BY es.TotalScore DESC;
END
GO

-- Tính điểm trung bình Student
CREATE PROCEDURE sp_GetStudentAverage
    @StudentId INT
AS
BEGIN
    SELECT 
        AVG(TotalScore) AS AverageScore,
        COUNT(*) AS TotalExams,
        MAX(TotalScore) AS HighestScore,
        MIN(TotalScore) AS LowestScore
    FROM ExamSubmissions
    WHERE StudentId = @StudentId AND Status = 'Graded';
END
GO
