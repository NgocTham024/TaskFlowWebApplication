using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Helpers;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            // tìm user bằng email or username
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == dto.EmailOrUsername || u.Username == dto.EmailOrUsername);

            if (user == null)
                return Unauthorized(new { message = "Username hoặc mật khẩu không chính xác. Vui lòng thử lại." });

            // verify password bằng BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!isPasswordValid)
                return Unauthorized(new { message = "Username hoặc mật khẩu không chính xác. Vui lòng thử lại." });

            // kiểm tra trạng thái tài khoản (email đã xác thực chưa)
            if (!user.IsEmailVerified)
                return Unauthorized(new { message = "Tài khoản chưa được xác thực. Vui lòng kiểm tra email." });

            // tạo JWT Access Token (hạn 15 phút)
            var accessToken = _tokenService.GenerateAccessToken(user);

            // tạo Refresh Token
            var refreshToken = _tokenService.GenerateRefreshToken();

            var jwtSettings = _configuration.GetSection("JwtSettings");
            var refreshTokenDays = int.Parse(jwtSettings["RefreshTokenExpirationDays"] ?? "3");
            var refreshExpiry = DateTime.UtcNow.AddDays(refreshTokenDays);

            // lưu Refresh Token dạng hash (SHA256) vào bảng RefreshTokens
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = RefreshTokenHelper.HashToken(refreshToken),
                ExpiresAt = refreshExpiry,
                RevokedAt = null
            };
            _context.RefreshTokens.Add(refreshTokenEntity);

            //cập nhật cả trên bảng Users
            user.RefreshToken = RefreshTokenHelper.HashToken(refreshToken);
            user.RefreshTokenExpiryTime = refreshExpiry;
            await _context.SaveChangesAsync();

            // gửi Refresh Token qua HttpOnly Cookie
            SetRefreshTokenCookie(refreshToken, refreshTokenDays);

            return Ok(new
            {
                accessToken,
                accessTokenExpiration = _tokenService.GetAccessTokenExpiration()
            });
        }

        // set refresh token vào HttpOnly Cookie
        private void SetRefreshTokenCookie(string refreshToken, int expirationDays)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(expirationDays)
            };

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
