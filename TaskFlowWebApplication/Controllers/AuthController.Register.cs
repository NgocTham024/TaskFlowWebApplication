using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Models;
using System.Security.Cryptography;
using System.Text.Encodings.Web;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            //kiểm tra email đã tồn tại chưa
            var emailExists = await _context.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists)
                return BadRequest(new { message = "Email đã tồn tại!" });

            // kiểm tra username đã tồn tại chưa
            var usernameExists = await _context.Users.AnyAsync(u => u.Username == dto.Username);
            if (usernameExists)
                return BadRequest(new { message = "Username đã tồn tại!" });

            //hash password bằng BCrypt
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // tạo email verification token
            var verificationToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            // tạo user mới (chưa xác thực email)
            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                Phone = dto.Phone,
                IsEmailVerified = 0,
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // gửi email xác thực
            var appUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7001";
            var encodedToken = Uri.EscapeDataString(verificationToken);
            var encodedEmail = Uri.EscapeDataString(user.Email);
            var verifyLink = $"{appUrl}/api/auth/verify-email?token={encodedToken}&email={encodedEmail}";

            var safeUsername = HtmlEncoder.Default.Encode(user.Username);
            var safeLink = HtmlEncoder.Default.Encode(verifyLink);

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #333;'>Xin chào {safeUsername},</h2>
                    <p>Cảm ơn bạn đã đăng ký tài khoản TaskFlow!</p>
                    <p>Vui lòng click vào nút bên dưới để xác thực email:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{safeLink}' 
                           style='background-color: #4CAF50; color: white; padding: 14px 28px; 
                                  text-decoration: none; border-radius: 5px; font-size: 16px;'>
                            Xác thực Email
                        </a>
                    </div>
                    <p style='color: #666; font-size: 14px;'>
                        Link xác thực có hiệu lực trong 24 giờ.<br/>
                        Nếu bạn không đăng ký tài khoản này, vui lòng bỏ qua email này.
                    </p>
                </div>";

            try
            {
                await _emailService.SendAsync(user.Email, "TaskFlow - Xác thực tài khoản", htmlBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể gửi email xác thực cho user {UserId}", user.UserId);
                return Ok(new
                {
                    emailSent = false,
                    message = "Tài khoản đã được tạo nhưng chưa gửi được email xác thực.",
                    emailError = string.Equals(
                        _configuration["ASPNETCORE_ENVIRONMENT"], "Development",
                        StringComparison.OrdinalIgnoreCase)
                        && ex is InvalidOperationException
                            ? ex.Message
                            : null
                });
            }

            return Ok(new
            {
                emailSent = true,
                message = "Đăng ký thành công! Vui lòng kiểm tra email để xác thực tài khoản."
            });
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string token, [FromQuery] string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == email &&
                u.EmailVerificationToken == token);

            if (user == null)
                return BadRequest(new { message = "Link xác thực không hợp lệ." });

            if (!user.EmailVerificationTokenExpires.HasValue
                || user.EmailVerificationTokenExpires.Value <= DateTime.UtcNow)
                return BadRequest(new { message = "Link xác thực đã hết hạn. Vui lòng đăng ký lại." });

            if (user.IsEmailVerified != 0)
                return Ok(new { message = "Email đã được xác thực trước đó." });

            //xác thực thành công
            user.IsEmailVerified = 1;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpires = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác thực email thành công! Bạn có thể đăng nhập." });
        }
    }
}
