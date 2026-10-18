using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Model;
using System.Security.Cryptography;

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
            var user = new Users
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = hashedPassword,
                Phone = dto.Phone,
                IsEmailVerified = false,
                EmailVerificationToken = verificationToken,
                EmailVerificationTokenExpires = DateTime.UtcNow.AddHours(24)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // gửi email xác thực
            var appUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7001";
            var verifyLink = $"{appUrl}/api/auth/verify-email?token={verificationToken}&email={user.Email}";

            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #333;'>Xin chào {user.Username},</h2>
                    <p>Cảm ơn bạn đã đăng ký tài khoản TaskFlow!</p>
                    <p>Vui lòng click vào nút bên dưới để xác thực email:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{verifyLink}' 
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
            catch (Exception)
            {
                //log lỗi gửi email nhưng vẫn tạo user thành công
                return Ok(new
                {
                    message = "Đăng ký thành công! Tuy nhiên không thể gửi email xác thực. Vui lòng liên hệ hỗ trợ."
                });
            }

            return Ok(new
            {
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

            if (user.EmailVerificationTokenExpires < DateTime.UtcNow)
                return BadRequest(new { message = "Link xác thực đã hết hạn. Vui lòng đăng ký lại." });

            if (user.IsEmailVerified)
                return Ok(new { message = "Email đã được xác thực trước đó." });

            //xác thực thành công
            user.IsEmailVerified = true;
            user.EmailVerificationToken = null;
            user.EmailVerificationTokenExpires = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác thực email thành công! Bạn có thể đăng nhập." });
        }
    }
}
