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
            // hash token đầu vào bằng SHA256
            var tokenHash = RefreshTokenHelper.HashToken(refreshToken);

            // tìm token khớp trực tiếp trong db
            var storedToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.TokenHash == tokenHash && t.RevokedAt == null);

            // kiểm tra token hợp lệ
            if (storedToken == null)
                return (false, "Refresh Token không hợp lệ hoặc đã bị thu hồi.", null, null);

            // kiểm tra token đã bị revoke
            if (storedToken.RevokedAt != null)
                return (false, "Refresh Token đã bị thu hồi.", null, null);

            // kiểm tra token hết hạn (> 3 ngày)
            if (storedToken.ExpiresAt <= DateTime.UtcNow)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return (false, "Refresh Token đã hết hạn. Vui lòng đăng nhập lại.", null, null);
            }

            // tìm user sở hữu token
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == storedToken.UserId);
            if (user == null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return (false, "Người dùng không tồn tại.", null, null);
            }

            // tạo Access Token mới
            // Lấy danh sách Role của người dùng từ database
            var roles = await (
                from userRole in _context.UserRoles
                join role in _context.Roles
                    on userRole.RoleId equals role.RoleId
                where userRole.UserId == user.UserId
                select role.RoleName
            )
            .Distinct()
            .ToListAsync();

            // Tạo Access Token mới có Role Claim
            string accessToken = _tokenService.GenerateAccessToken(user, roles);

            DateTime expiresAt = _tokenService.GetAccessTokenExpiration();

            return (true, "Làm mới Access Token thành công.", accessToken, expiresAt);
        }
    }
}
