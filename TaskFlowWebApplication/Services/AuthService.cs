using Microsoft.EntityFrameworkCore;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.Helpers;

namespace TaskFlowWebApplication.Services
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext context, ITokenService tokenService, IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _config = config;
        }

        public async Task<(bool Success, string Message, string? AccessToken, DateTime? ExpiresAt)> RefreshTokenAsync(string refreshToken)
        {
            // 1. Lấy tất cả token chưa bị revoke
            var candidateTokens = await _context.RefreshTokens
                .Where(t => t.RevokedAt == null)
                .ToListAsync();

            // 2. Tìm token khớp bằng BCrypt verify
            var storedToken = candidateTokens.FirstOrDefault(
                t => RefreshTokenHelper.VerifyToken(refreshToken, t.TokenHash));

            // 3. Kiểm tra token hợp lệ
            if (storedToken == null)
                return (false, "Refresh Token không hợp lệ hoặc đã bị thu hồi.", null, null);

            // 4. Kiểm tra token đã bị revoke
            if (storedToken.RevokedAt != null)
                return (false, "Refresh Token đã bị thu hồi.", null, null);

            // 5. Kiểm tra token hết hạn (> 3 ngày)
            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return (false, "Refresh Token đã hết hạn. Vui lòng đăng nhập lại.", null, null);
            }

            // 6. Tìm user sở hữu token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == storedToken.UserId);
            if (user == null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return (false, "Người dùng không tồn tại.", null, null);
            }

            // 7. Tạo Access Token mới
            string accessToken = _tokenService.GenerateAccessToken(user);
            DateTime expiresAt = _tokenService.GetAccessTokenExpiration();

            return (true, "Làm mới Access Token thành công.", accessToken, expiresAt);
        }
    }
}
