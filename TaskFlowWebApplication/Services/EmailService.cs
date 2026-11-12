using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TaskFlowWebApplication.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var settings = _config.GetSection("EmailSettings");
            var host = settings["Host"]?.Trim();
            var senderEmail = settings["SenderEmail"]?.Trim();
            var username = settings["Username"]?.Trim();
            var password = settings["Password"];

            if (string.IsNullOrWhiteSpace(host))
                throw new InvalidOperationException("EmailSettings:Host chưa được cấu hình.");
            if (!int.TryParse(settings["Port"], out var port) || port < 1 || port > 65535)
                throw new InvalidOperationException("EmailSettings:Port phải nằm trong khoảng 1–65535.");
            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(username)
                || string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Thiếu SenderEmail, Username hoặc Password trong EmailSettings.");
            if (!MailboxAddress.TryParse(senderEmail, out var sender)
                || !MailboxAddress.TryParse(toEmail, out var recipient))
                throw new InvalidOperationException("Địa chỉ email người gửi hoặc người nhận không hợp lệ.");

            // Gmail hiển thị App Password theo nhóm có khoảng trắng.
            // Không chuẩn hóa mật khẩu của các SMTP server khác.
            if (string.Equals(host, "smtp.gmail.com", StringComparison.OrdinalIgnoreCase))
                password = password.Replace(" ", "").Trim();

            sender.Name = settings["SenderName"] ?? "TaskFlow";
            var message = new MimeMessage();
            message.From.Add(sender);
            message.To.Add(recipient);
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            var security = port == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            using var client = new SmtpClient { Timeout = 15000 };
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            var stage = "kết nối TLS";
            try
            {
                await client.ConnectAsync(host, port, security, timeout.Token);
                stage = "xác thực SMTP";
                await client.AuthenticateAsync(username, password, timeout.Token);
                stage = "gửi thư";
                await client.SendAsync(message, timeout.Token);
            }
            catch (MailKit.Security.AuthenticationException ex)
            {
                _logger.LogError("SMTP authentication failed for {Host}:{Port} ({ErrorType}).",
                    host, port, ex.GetType().Name);
                throw new InvalidOperationException(
                    "SMTP từ chối đăng nhập. Kiểm tra Username và Password trong cấu hình đang chạy; " +
                    "Gmail cần App Password. Trong Development, User Secrets ghi đè appsettings.json.");
            }
            catch (Exception ex) when (ex is IOException || ex is System.Net.Sockets.SocketException
                || ex is OperationCanceledException || ex is SmtpProtocolException
                || ex is SslHandshakeException)
            {
                _logger.LogError("SMTP failed at {Stage} for {Host}:{Port} ({ErrorType}).",
                    stage, host, port, ex.GetType().Name);
                throw new InvalidOperationException(
                    $"Không hoàn tất bước {stage} với SMTP {host}:{port}. Kiểm tra mạng và cấu hình TLS. " +
                    "Nếu kết nối mất sau khi gửi, hãy kiểm tra hộp thư trước khi thử lại.");
            }
            catch (SmtpCommandException ex)
            {
                _logger.LogError("SMTP rejected message with status {StatusCode} at {Stage}.",
                    (int)ex.StatusCode, stage);
                throw new InvalidOperationException(
                    $"SMTP từ chối yêu cầu, mã {(int)ex.StatusCode}. Kiểm tra địa chỉ gửi, nhận và giới hạn gửi thư.");
            }
            finally
            {
                if (client.IsConnected)
                {
                    try
                    {
                        using var disconnectTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                        await client.DisconnectAsync(true, disconnectTimeout.Token);
                    }
                    catch (Exception ex)
                    {
                        // Lỗi đóng kết nối không được biến thư đã gửi thành kết quả thất bại.
                        _logger.LogWarning("SMTP disconnect failed ({ErrorType}).", ex.GetType().Name);
                    }
                }
            }
        }
    }
}
