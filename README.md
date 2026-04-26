# 📚 ExamSystem - Hệ thống Quản lý và Thi trực tuyến

Hệ thống thi trực tuyến xây dựng bằng **ASP.NET Core 8 MVC** + **SQL Server** + **Entity Framework Core**.

---

## 🚀 Cách cài đặt và chạy

### Yêu cầu
- .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
- SQL Server 2019+ hoặc SQL Server Express
- Visual Studio 2022 hoặc VS Code

---

### Bước 1: Tạo Database
Mở **SQL Server Management Studio (SSMS)**, chạy file:
```
Database/schema.sql
```
File này sẽ tự tạo database `ExamSystemDB` và chèn dữ liệu mẫu.

---

### Bước 2: Cấu hình Connection String
Mở `appsettings.json`, sửa phần ConnectionString:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.\\SQLEXPRESS;Database=ExamSystemDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

**Các dạng Connection String phổ biến:**
| Server | Connection String |
|--------|-------------------|
| SQL Server Express (local) | `Server=.\\SQLEXPRESS;Database=ExamSystemDB;Trusted_Connection=True;TrustServerCertificate=True;` |
| SQL Server (local) | `Server=.;Database=ExamSystemDB;Trusted_Connection=True;TrustServerCertificate=True;` |
| SQL Server với auth | `Server=localhost;Database=ExamSystemDB;User Id=sa;Password=yourpassword;TrustServerCertificate=True;` |

---

### Bước 3: Cài Packages và chạy
```bash
cd ExamSystem
dotnet restore
dotnet run
```

Sau đó mở trình duyệt: `https://localhost:5001` hoặc `http://localhost:5000`

---

## 🔑 Tài khoản mặc định

| Vai trò | Username | Mật khẩu |
|---------|----------|-----------|
| Admin | `admin` | `Admin@123` |
| Giáo viên | `GV101` | `Admin@123` |
| Sinh viên | `20231234` | `Admin@123` |

---

## 📁 Cấu trúc dự án

```
ExamSystem/
├── Controllers/
│   ├── AccountController.cs    # Đăng nhập, đăng xuất, hồ sơ
│   ├── AdminController.cs      # Quản lý tài khoản
│   ├── TeacherController.cs    # Quản lý đề thi, câu hỏi, chấm bài
│   ├── StudentController.cs    # Trang sinh viên, luyện tập
│   └── ExamController.cs       # Làm bài thi, nộp bài, kết quả
├── Models/
│   └── Models.cs               # Tất cả Entity models
├── ViewModels/
│   └── ViewModels.cs           # ViewModels cho tất cả tính năng
├── Data/
│   └── ExamDbContext.cs        # Entity Framework DbContext
├── Views/
│   ├── Shared/_Layout.cshtml   # Layout chung với sidebar
│   ├── Account/                # Login, Profile, ChangePassword
│   ├── Admin/                  # Dashboard, Users
│   ├── Teacher/                # Dashboard, Exams, EditExam, Grade
│   ├── Student/                # Dashboard, History, Practice
│   └── Exam/                   # Enter, Take, Result
├── Database/
│   └── schema.sql              # SQL Server schema + seed data
├── Program.cs
├── appsettings.json
└── ExamSystem.csproj
```

---

## ✅ Tính năng đã implement

### 👤 Chung (User)
- [x] Đăng nhập / Đăng xuất
- [x] Xem và cập nhật hồ sơ cá nhân
- [x] Đổi mật khẩu (yêu cầu nhập mật khẩu cũ)
- [x] Phân quyền theo vai trò (Admin/Teacher/Student)

### 🔧 Admin
- [x] Dashboard thống kê
- [x] Tạo tài khoản Giáo viên / Sinh viên
- [x] Xem danh sách, lọc, tìm kiếm tài khoản
- [x] Chỉnh sửa thông tin tài khoản
- [x] Khóa / Mở khóa tài khoản
- [x] Xóa tài khoản
- [x] Đặt lại mật khẩu

### 👨‍🏫 Giáo viên
- [x] Dashboard tổng quan
- [x] Tạo, chỉnh sửa, xóa đề thi
- [x] Công bố / Ẩn đề thi
- [x] Thêm, sửa, xóa câu hỏi (Trắc nghiệm, Đúng/Sai, Tự luận)
- [x] Xem danh sách bài nộp
- [x] Chấm bài tự luận + thêm nhận xét
- [x] Thống kê kết quả lớp

### 🎓 Sinh viên
- [x] Dashboard với thống kê cá nhân
- [x] Xem danh sách đề thi
- [x] Nhập mật khẩu bài thi
- [x] Làm bài thi với đồng hồ đếm ngược
- [x] Auto-save câu trả lời
- [x] Tự động nộp khi hết giờ
- [x] Xem kết quả và đáp án (nếu được phép)
- [x] Xem nhận xét của giáo viên
- [x] Lịch sử bài thi
- [x] Tạo và làm đề luyện tập

---

## 🔒 Bảo mật
- Mật khẩu mã hóa bằng **BCrypt**
- Xác thực bằng **Cookie Authentication**
- Phân quyền bằng **[Authorize(Roles)]**
- Anti-forgery token trên tất cả form POST
- NFR6: Thời gian đồng bộ với server

---

## 📦 NuGet Packages
```
Microsoft.EntityFrameworkCore.SqlServer 8.0.0
Microsoft.EntityFrameworkCore.Tools 8.0.0
BCrypt.Net-Next 4.0.3
Microsoft.AspNetCore.Authentication.Cookies 2.2.0
```
