using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        [AllowAnonymous]
        [EnableRateLimiting("LoginLimiter")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            // tìm user bằng email HOẶC username
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

            // verify password bằng BCrypt
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

            // kiểm tra trạng thái tài khoản
            if (user.IsEmailVerified == 0)
            {
                return Unauthorized(new
                {
                    message = "Tài khoản chưa được xác thực. Vui lòng kiểm tra email."
                });
            }

            // kiểm tra trạng thái 2FA
            var twoFactor = await _context.UserTwoFactors
                .FirstOrDefaultAsync(x => x.UserId == user.UserId);

            if ((twoFactor != null && twoFactor.IsEnabled == 1) || user.TwoFactorEnabled == 1)
            {
                var challengeToken = await _twoFactorService.CreateLoginChallengeAsync(user.UserId);

                return Ok(new
                {
                    requiresTwoFactor = true,
                    challengeToken,
                    message = "Vui lòng nhập mã OTP để hoàn tất đăng nhập."
                });
            }

            // hoàn tất đăng nhập
            return await CompleteLoginAsync(user);
        }

        private async Task<IActionResult> CompleteLoginAsync(User user)
        {
            // lấy danh sách Role từ database
            var roles = await (
                from userRole in _context.UserRoles
                join role in _context.Roles
                    on userRole.RoleId equals role.Id
                where userRole.UserId == user.UserId
                select role.Name
            ).Distinct().ToListAsync();

            if (roles.Count == 0)
            {
                return StatusCode(403, new
                {
                    message = "Tài khoản chưa được cấp quyền."
                });
            }

            // tạo JWT Access Token chứa Role Claim
            var accessToken = _tokenService.GenerateAccessToken(user, roles);

            // tạo Refresh Token
            var refreshToken = _tokenService.GenerateRefreshToken();

            var jwtSettings = _configuration.GetSection("JwtSettings");

            var refreshTokenDays = int.Parse(
                jwtSettings["RefreshTokenExpirationDays"] ?? "3"
            );

            var refreshExpiry = DateTime.UtcNow.AddDays(refreshTokenDays);

            // lưu Refresh Token dạng SHA256 hash
            var tokenHash = RefreshTokenHelper.HashToken(refreshToken);

            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = tokenHash,
                ExpiresAt = refreshExpiry,
                RevokedAt = null
            };

            _context.RefreshTokens.Add(refreshTokenEntity);

            // cập nhật bảng Users
            user.RefreshToken = tokenHash;
            user.RefreshTokenExpiryTime = refreshExpiry;

            await _context.SaveChangesAsync();

            // gửi Refresh Token qua HttpOnly Cookie
            SetRefreshTokenCookie(refreshToken, refreshTokenDays);

            // trả Access Token về frontend
            return Ok(new
            {
                requiresTwoFactor = false,
                accessToken,
                accessTokenExpiration = _tokenService.GetAccessTokenExpiration()
            });
        }

        // set Refresh Token vào HttpOnly Cookie
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
