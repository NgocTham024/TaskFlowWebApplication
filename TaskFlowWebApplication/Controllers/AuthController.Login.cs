
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
            if (!user.IsEmailVerified)
            {
                return Unauthorized(new
                {
                    message = "Tài khoản chưa được xác thực. Vui lòng kiểm tra email."
                });
            }

            // lấy danh sách Role từ database
            var roles = await (
                from userRole in _context.UserRoles
                join role in _context.Roles
                    on userRole.RoleId equals role.RoleId
                where userRole.UserId == user.UserId
                select role.RoleName
            ).Distinct() .ToListAsync();

            // kiểm tra tài khoản đã được phân quyền
            if (roles.Count == 0)
            {
                return StatusCode(403, new
                {
                    message = "Tài khoản chưa được cấp quyền."
                });
            }

            // tạo JWT Access Token chứa Role Claim
            var accessToken = _tokenService.GenerateAccessToken(
                user,
                roles
            );

            // tạo Refresh Token
            var refreshToken = _tokenService.GenerateRefreshToken();

            var jwtSettings = _configuration.GetSection("JwtSettings");

            var refreshTokenDays = int.Parse(
                jwtSettings["RefreshTokenExpirationDays"] ?? "3"
            );

            var refreshExpiry = DateTime.UtcNow.AddDays(
                refreshTokenDays
            );

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
                accessToken, accessTokenExpiration = _tokenService.GetAccessTokenExpiration()
            });
        }

        //set Refresh Token vào HttpOnly Cookie
        private void SetRefreshTokenCookie(
            string refreshToken,
            int expirationDays)
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
