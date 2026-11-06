using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Helpers;
using TaskFlowWebApplication.Models;
using Microsoft.AspNetCore.RateLimiting;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("login")]
        [EnableRateLimiting("LoginLimit")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            //tìm User bằng Email hoặc Username
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == dto.EmailOrUsername ||
                u.Username == dto.EmailOrUsername);

            if (user == null)
                return Unauthorized(new
                {
                    message = "Sai email/username hoặc mật khẩu"
                });

            //xác minh Password
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(
                dto.Password,
                user.PasswordHash);

            if (!isPasswordValid)
                return Unauthorized(new
                {
                    message = "Sai email/username hoặc mật khẩu"
                });

            //kiểm tra Email
            if (!user.IsEmailVerified)
                return Unauthorized(new
                {
                    message = "Tài khoản chưa được xác thực."
                });

            //kiểm tra trạng thái 2FA
            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x => x.UserId == user.UserId);

            if (twoFactor?.IsEnabled == true)
            {
                var challengeToken =
                    await _twoFactorService.CreateLoginChallengeAsync(user.UserId);

                return Ok(new
                {
                    requiresTwoFactor = true,
                    challengeToken,
                    message = "Vui lòng nhập mã OTP để hoàn tất đăng nhập."
                });
            }

            // 5. Nếu chưa bật 2FA, đăng nhập như bình thường
            return await CompleteLoginAsync(user);
        }

        // Hoàn tất đăng nhập và cấp Token
        private async Task<IActionResult> CompleteLoginAsync(
            TaskFlowWebApplication.Model.Users user)
        {
            var accessToken = _tokenService.GenerateAccessToken(user);

            var refreshToken = _tokenService.GenerateRefreshToken();

            var jwtSettings = _configuration.GetSection("JwtSettings");

            var refreshTokenDays = int.Parse(
                jwtSettings["RefreshTokenExpirationDays"] ?? "3");

            var refreshExpiry = DateTime.UtcNow.AddDays(
                refreshTokenDays);

            var hashedRefreshToken =
                RefreshTokenHelper.HashToken(refreshToken);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = hashedRefreshToken,
                ExpiresAt = refreshExpiry,
                RevokedAt = null
            };

            _context.RefreshTokens.Add(refreshTokenEntity);

            user.RefreshToken = hashedRefreshToken;
            user.RefreshTokenExpiryTime = refreshExpiry;

            await _context.SaveChangesAsync();

            SetRefreshTokenCookie(refreshToken, refreshTokenDays);

            return Ok(new
            {
                requiresTwoFactor = false,
                accessToken,
                accessTokenExpiration =
                    _tokenService.GetAccessTokenExpiration()
            });
        }

        // Refresh Token HttpOnly Cookie
        private void SetRefreshTokenCookie(
            string refreshToken,
            int expirationDays)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(expirationDays),
                Path = "/"
            };

            Response.Cookies.Append(
                "refreshToken",
                refreshToken,
                cookieOptions);
        }
    }
}
