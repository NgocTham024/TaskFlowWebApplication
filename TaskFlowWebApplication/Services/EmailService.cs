using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace TaskFlowWebApplication.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config) => _config = config;

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var s = _config.GetSection("EmailSettings");
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(s["SenderName"] ?? "TaskFlow", s["SenderEmail"]!));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(s["Host"]!, int.Parse(s["Port"]!), SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(s["Username"]!, s["Password"]!);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
