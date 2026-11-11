
using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.Helpers;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public AuthService(
            AppDbContext context,
            ITokenService tokenService,
            IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _config = config;
        }

        public async Task<(
            bool Success,
            string Message,
            string? AccessToken,
            DateTime? ExpiresAt,
            string? RefreshToken,
            int RefreshTokenExpirationDays
        )> RefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return (
                    false,
                    "Refresh Token không hợp lệ.",
                    null, null, null, 0
                );
            }

            var tokenHash = RefreshTokenHelper.HashToken(refreshToken);

            // Tìm Refresh Token cũ
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

            if (storedToken == null)
            {
                return (
                    false,
                    "Refresh Token không hợp lệ.",
                    null, null, null, 0
                );
            }

            // Kiểm tra token đã bị thu hồi
            if (storedToken.RevokedAt != null)
            {
                return (
                    false,
                    "Refresh Token đã bị thu hồi.",
                    null, null, null, 0
                );
            }

            // Kiểm tra hết hạn
            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return (
                    false,
                    "Refresh Token đã hết hạn.",
                    null, null, null, 0
                );
            }

            // Tìm User
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == storedToken.UserId);

            if (user == null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return (
                    false,
                    "Người dùng không tồn tại.",
                    null, null, null, 0
                );
            }

            // Đọc thời hạn Refresh Token từ cấu hình
            var jwtSettings = _config.GetSection("JwtSettings");

            var refreshTokenDays = int.Parse(
                jwtSettings["RefreshTokenExpirationDays"] ?? "3");

            if (refreshTokenDays <= 0)
            {
                throw new InvalidOperationException(
                    "RefreshTokenExpirationDays phải lớn hơn 0.");
            }

            // Tạo Access Token mới
            var newAccessToken = _tokenService.GenerateAccessToken(user);
            var accessTokenExpiry = _tokenService.GetAccessTokenExpiration();

            // Tạo Refresh Token mới
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            var newRefreshTokenHash =
                RefreshTokenHelper.HashToken(newRefreshToken);

            var newRefreshExpiry =
                DateTime.UtcNow.AddDays(refreshTokenDays);

            // Thu hồi Refresh Token cũ
            storedToken.RevokedAt = DateTime.UtcNow;

            // Lưu Refresh Token mới
            var newTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = newRefreshTokenHash,
                ExpiresAt = newRefreshExpiry,
                RevokedAt = null
            };

            _context.RefreshTokens.Add(newTokenEntity);

            // Cập nhật Refresh Token trong Users
            user.RefreshToken = newRefreshTokenHash;
            user.RefreshTokenExpiryTime = newRefreshExpiry;

            await _context.SaveChangesAsync();

            return (
                true,
                "Làm mới Access Token thành công.",
                newAccessToken,
                accessTokenExpiry,
                newRefreshToken,
                refreshTokenDays
            );
        }
    }
}
