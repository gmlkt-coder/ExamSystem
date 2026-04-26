using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ExamSystem.Data;
using ExamSystem.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllersWithViews();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

// Database
builder.Services.AddDbContext<ExamDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExamDbContext>();
    db.Database.ExecuteSqlRaw("""
        IF COL_LENGTH('Users', 'MustChangePassword') IS NULL
        BEGIN
            ALTER TABLE Users ADD MustChangePassword bit NOT NULL CONSTRAINT DF_Users_MustChangePassword DEFAULT 0;
        END

        IF COL_LENGTH('Users', 'IsActivated') IS NULL
        BEGIN
            ALTER TABLE Users ADD IsActivated bit NOT NULL CONSTRAINT DF_Users_IsActivated DEFAULT 0;
        END

        UPDATE Users
        SET IsActivated = 1
        WHERE Role = 'Admin';

        IF OBJECT_ID('PasswordResetRequests', 'U') IS NULL
        BEGIN
            CREATE TABLE PasswordResetRequests (
                PasswordResetRequestId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                UserId INT NOT NULL,
                Status NVARCHAR(20) NOT NULL DEFAULT N'Pending',
                Email NVARCHAR(100) NULL,
                Message NVARCHAR(MAX) NULL,
                AdminNote NVARCHAR(MAX) NULL,
                RequestedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                ProcessedAt DATETIME2 NULL,
                CONSTRAINT FK_PasswordResetRequests_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
            );
            CREATE INDEX IX_PasswordResetRequests_UserId ON PasswordResetRequests(UserId);
            CREATE INDEX IX_PasswordResetRequests_Status ON PasswordResetRequests(Status);
        END

        IF OBJECT_ID('EmailVerificationTokens', 'U') IS NULL
        BEGIN
            CREATE TABLE EmailVerificationTokens (
                EmailVerificationTokenId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                UserId INT NOT NULL,
                Purpose NVARCHAR(20) NOT NULL,
                CodeHash NVARCHAR(256) NOT NULL,
                Email NVARCHAR(100) NOT NULL,
                VerificationKey NVARCHAR(64) NOT NULL,
                ExpiresAt DATETIME2 NOT NULL,
                CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                VerifiedAt DATETIME2 NULL,
                ConsumedAt DATETIME2 NULL,
                FailedAttempts INT NOT NULL DEFAULT 0,
                CONSTRAINT FK_EmailVerificationTokens_Users_UserId FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
            );
            CREATE INDEX IX_EmailVerificationTokens_UserId ON EmailVerificationTokens(UserId);
            CREATE INDEX IX_EmailVerificationTokens_VerificationKey ON EmailVerificationTokens(VerificationKey);
        END
        """);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
