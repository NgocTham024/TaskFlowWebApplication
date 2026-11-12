using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.DTOs;
using TaskFlowWebApplication.Helpers;
using TaskFlowWebApplication.Model;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Controller
{
    public partial class AuthController
    {
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            // 1. Tìm user bằng email HOẶC username
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.Email == dto.EmailOrUsername ||
                u.Username == dto.EmailOrUsername);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Sai email/username hoặc mật khẩu"
                });
            }

            // 2. Verify password bằng BCrypt
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(
                dto.Password,
                user.PasswordHash
            );

            if (!isPasswordValid)
            {
                return Unauthorized(new
                {
                    message = "Sai email/username hoặc mật khẩu"
                });
            }

            // 3. Kiểm tra trạng thái tài khoản (email đã xác thực chưa)
            if (!user.IsEmailVerified)
            {
                return Unauthorized(new
                {
                    message = "Tài khoản chưa được xác thực. Vui lòng kiểm tra email."
                });
            }

            // 4. Kiểm tra trạng thái 2FA
            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x => x.UserId == user.UserId);

            if (twoFactor?.IsEnabled == true)
            {
                var challengeToken = await _twoFactorService.CreateLoginChallengeAsync(user.UserId);

                return Ok(new
                {
                    requiresTwoFactor = true,
                    challengeToken,
                    message = "Vui lòng nhập mã OTP để hoàn tất đăng nhập."
                });
            }

            // 5. Nếu không bật 2FA, hoàn tất đăng nhập bình thường
            return await CompleteLoginAsync(user);
        }

        // Hoàn tất đăng nhập và cấp Token
        private async Task<IActionResult> CompleteLoginAsync(Users user)
        {
            // Tạo JWT Access Token
            var accessToken = _tokenService.GenerateAccessToken(user);

            // Tạo Refresh Token
            var refreshToken = _tokenService.GenerateRefreshToken();

            var jwtSettings = _configuration.GetSection("JwtSettings");

            var refreshTokenDays = int.Parse(
                jwtSettings["RefreshTokenExpirationDays"] ?? "3"
            );

            var refreshExpiry = DateTime.UtcNow.AddDays(refreshTokenDays);

            // Lưu Refresh Token dạng SHA256 hash
            var tokenHash = RefreshTokenHelper.HashToken(refreshToken);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = tokenHash,
                ExpiresAt = refreshExpiry,
                RevokedAt = null
            };

            _context.RefreshTokens.Add(refreshTokenEntity);

            // Cập nhật bảng Users
            user.RefreshToken = tokenHash;
            user.RefreshTokenExpiryTime = refreshExpiry;

            await _context.SaveChangesAsync();

            // Gửi Refresh Token qua HttpOnly Cookie
            SetRefreshTokenCookie(refreshToken, refreshTokenDays);

            // Trả Access Token về frontend
            return Ok(new
            {
                requiresTwoFactor = false,
                accessToken,
                accessTokenExpiration = _tokenService.GetAccessTokenExpiration()
            });
        }

        // Set Refresh Token vào HttpOnly Cookie
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

            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }
    }
}
