using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ExamSystem.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;

        public SmtpEmailSender(IOptions<EmailSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            ValidateSettings();

            using var message = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };

            message.To.Add(toEmail);

            using var client = new SmtpClient(_settings.Host, _settings.Port)
            {
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                EnableSsl = _settings.EnableSsl
            };

            try
            {
                await client.SendMailAsync(message);
            }
            catch (SmtpException ex)
            {
                throw new InvalidOperationException(BuildFriendlySmtpMessage(ex), ex);
            }
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.Host)
                || string.IsNullOrWhiteSpace(_settings.Username)
                || string.IsNullOrWhiteSpace(_settings.Password)
                || string.IsNullOrWhiteSpace(_settings.SenderEmail))
            {
                throw new InvalidOperationException("Cấu hình email SMTP chưa đầy đủ. Hãy kiểm tra lại `EmailSettings` trong `appsettings.json`.");
            }

            if (IsPlaceholder(_settings.SenderEmail) || IsPlaceholder(_settings.Username) || IsPlaceholder(_settings.Password))
            {
                throw new InvalidOperationException(
                    "Bạn chưa thay cấu hình mẫu trong `appsettings.json`. " +
                    "Hãy điền Gmail thật vào `SenderEmail`/`Username` và App Password 16 ký tự của Google vào `Password`.");
            }

            if (_settings.Host.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase)
                && !_settings.EnableSsl)
            {
                throw new InvalidOperationException("Gmail SMTP yêu cầu bật SSL/TLS. Hãy đặt `EnableSsl = true`.");
            }
        }

        private static bool IsPlaceholder(string value)
        {
            var normalized = value.Trim().ToLowerInvariant();
            return normalized.Contains("your-gmail")
                || normalized.Contains("your-app-password")
                || normalized.Contains("example")
                || normalized == "changeme";
        }

        private string BuildFriendlySmtpMessage(SmtpException ex)
        {
            var raw = ex.ToString();
            var message = new StringBuilder();

            if (_settings.Host.Equals("smtp.gmail.com", StringComparison.OrdinalIgnoreCase)
                && raw.Contains("Authentication Required", StringComparison.OrdinalIgnoreCase))
            {
                message.Append("Gmail từ chối đăng nhập SMTP. ");
                message.Append("Bạn cần bật `2-Step Verification` cho tài khoản Google và tạo `App Password`. ");
                message.Append("Sau đó dán App Password 16 ký tự vào `EmailSettings:Password` trong `appsettings.json`.");
                return message.ToString();
            }

            if (raw.Contains("secure connection", StringComparison.OrdinalIgnoreCase))
            {
                message.Append("Máy chủ SMTP yêu cầu kết nối bảo mật. ");
                message.Append("Hãy kiểm tra `Host`, `Port`, `EnableSsl` và thông tin đăng nhập email.");
                return message.ToString();
            }

            if (raw.Contains("mailbox unavailable", StringComparison.OrdinalIgnoreCase)
                || raw.Contains("recipient", StringComparison.OrdinalIgnoreCase))
            {
                message.Append("Địa chỉ email người nhận không hợp lệ hoặc không tồn tại.");
                return message.ToString();
            }

            return $"Không gửi được email SMTP. Chi tiết: {ex.Message}";
        }
    }
}
