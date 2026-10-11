using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            //tìm user bằng email or username
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == dto.EmailOrUsername || u.Username == dto.EmailOrUsername);

            if (user == null)
                return Unauthorized(new { message = "Sai email/username hoặc mật khẩu" });

            // verify password bằng BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
                return Unauthorized(new { message = "Sai email/username hoặc mật khẩu" });

            // kiểm tra trạng thái tài khoản (email đã xác thực chưa)
            if (!user.IsEmailVerified)
                return Unauthorized(new { message = "Tài khoản chưa được xác thực. Vui lòng kiểm tra email." });

            //tạo JWT Access Token (hạn 15 phút)
            var accessToken = _tokenService.GenerateAccessToken(user);

            // tạo Refresh Token
            var refreshToken = _tokenService.GenerateRefreshToken();

            // lưu Refresh Token dạng HASH vào database (SHA256)
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var refreshTokenDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "3");

            user.RefreshToken = HashToken(refreshToken);
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(refreshTokenDays);
            await _context.SaveChangesAsync();

            // gửi Refresh Token qua HttpOnly Cookie
            SetRefreshTokenCookie(refreshToken, refreshTokenDays);

            return Ok(new
            {
                accessToken,
                accessTokenExpiration = DateTime.UtcNow.AddMinutes(
                    int.Parse(jwtSettings["AccessTokenExpirationMinutes"] ?? "15"))
            });
        }

        // hash token bằng SHA256 trước khi lưu db
        private static string HashToken(string token)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(bytes);
        }

        // set refresh token vào HttpOnly Cookie
        private void SetRefreshTokenCookie(string refreshToken, int expirationDays)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,    // JavaScript không thể đọc
                Secure = true,      // Chỉ gửi qua HTTPS
                SameSite = SameSiteMode.Strict, // Chống CSRF
                Expires = DateTime.UtcNow.AddDays(expirationDays)
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
