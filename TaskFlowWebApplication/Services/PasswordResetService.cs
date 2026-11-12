
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Services
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly AppDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public PasswordResetService(
            AppDbContext context,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        // FORGOT PASSWORD
        public async Task RequestPasswordResetAsync(string email)
        {
            //kiểm tra và chuẩn hóa Email
            if (string.IsNullOrWhiteSpace(email))
                return;

            email = email.Trim().ToLowerInvariant();

            //tìm người dùng theo Email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
                return;

            //kiểm tra Frontend URL
            var frontendUrl = _configuration["FrontendUrl"];

            if (string.IsNullOrWhiteSpace(frontendUrl) ||
                !Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontendUri) ||
                (frontendUri.Scheme != Uri.UriSchemeHttps &&
                 frontendUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new InvalidOperationException(
                    "FrontendUrl chưa được cấu hình hợp lệ.");
            }

            //tạo Reset Token ngẫu nhiên
            var tokenBytes = RandomNumberGenerator.GetBytes(32);

            var resetToken = Convert.ToBase64String(tokenBytes)
                .Replace("+", "-")
                .Replace("/", "_")
                .TrimEnd('=');

            //hash Reset Token bằng SHA256
            var tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(resetToken)));

            //vô hiệu hóa các Reset Token cũ chưa sử dụng
            var oldTokens = await _context.PasswordResetTokens
                .Where(t =>
                    t.UserId == user.UserId &&
                    t.UsedAt == null)
                .ToListAsync();

            var now = DateTime.UtcNow;

            foreach (var oldToken in oldTokens)
            {
                oldToken.UsedAt = now;
            }

            //tạo Reset Token mới
            var resetTokenEntity = new PasswordResetToken
            {
                UserId = user.UserId,
                TokenHash = tokenHash,
                ExpiresAt = now.AddMinutes(30),
                CreatedAt = now
            };

            _context.PasswordResetTokens.Add(resetTokenEntity);

            //lưu Token vào Database
            await _context.SaveChangesAsync();

            //tạo đường link Reset Password
            var resetLink =
                $"{frontendUri.AbsoluteUri.TrimEnd('/')}/reset-password" +
                $"?token={Uri.EscapeDataString(resetToken)}";

            //tạo nội dung Email
            var htmlBody = $@"
                <h2>TaskFlow - Reset Password</h2>

                <p>Hello {System.Net.WebUtility.HtmlEncode(user.Username)},</p>

                <p>You requested to reset your password.</p>

                <p>This link will expire in 30 minutes.</p>

                <p>
                    <a href=""{System.Net.WebUtility.HtmlEncode(resetLink)}"">
                        Reset Password
                    </a>
                </p>

                <p>If you did not request this, please ignore this email.</p>
            ";

            //gửi Email
            await _emailService.SendAsync(
                user.Email,
                "TaskFlow - Password Reset",
                htmlBody
            );
        }

        // RESET PASSWORD
        public async Task<bool> ResetPasswordAsync(
            string token,
            string newPassword)
        {
            //kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(token) ||
                string.IsNullOrWhiteSpace(newPassword))
            {
                return false;
            }

            //hash Reset Token nhận từ Frontend
            var tokenHash = Convert.ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(token)));

            //tìm Reset Token trong Database
            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.User)
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            //kiểm tra Token có tồn tại không
            if (resetToken == null)
                return false;

            //kiểm tra User có tồn tại không
            if (resetToken.User == null)
                return false;

            //kiểm tra Token đã sử dụng chưa
            if (resetToken.UsedAt != null)
                return false;

            //kiểm tra Token hết hạn chưa
            if (resetToken.ExpiresAt <= DateTime.UtcNow)
                return false;

            //hash mật khẩu mới bằng BCrypt
            resetToken.User.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(newPassword);

            var now = DateTime.UtcNow;

            //đánh dấu Reset Token đã sử dụng
            resetToken.UsedAt = now;

            //thu hồi Refresh Token hiện tại
            resetToken.User.RefreshToken = null;
            resetToken.User.RefreshTokenExpiryTime = null;

            //vô hiệu hóa những Reset Token khác của User
            var otherTokens = await _context.PasswordResetTokens
                .Where(t =>
                    t.UserId == resetToken.UserId &&
                    t.Id != resetToken.Id &&
                    t.UsedAt == null)
                .ToListAsync();

            foreach (var item in otherTokens)
            {
                item.UsedAt = now;
            }

            //lưu tất cả thay đổi vào Database
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
