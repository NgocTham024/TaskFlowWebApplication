using System.Security.Cryptography;
using System.Text;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.Helpers;
using TaskFlowWebApplication.Model;
using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Services
{
    public record IssuedTokens(string AccessToken, DateTime AccessTokenExpiration, string RefreshToken, DateTime RefreshTokenExpiration);

    public class TokenIssue : ITokenIssue
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public TokenIssue(AppDbContext context, ITokenService tokenService, IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _config = config;
        }

        public async Task<IssuedTokens> IssueAsync(Users user)
        {
            var jwt = _config.GetSection("JwtSettings");
            var accessMinutes = int.Parse(jwt["AccessTokenExpirationMinutes"] ?? "15");
            var refreshDays = int.Parse(jwt["RefreshTokenExpirationDays"] ?? "3");

            // Tạo tokens
            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshExpiry = DateTime.UtcNow.AddDays(refreshDays);

            // Lưu hash refresh token vào bảng Users
            user.RefreshToken = RefreshTokenHelper.HashToken(refreshToken);
            user.RefreshTokenExpiryTime = refreshExpiry;

            // Lưu hash refresh token vào bảng RefreshTokens
            var refreshTokenEntity = new RefreshToken
            {
                UserId = user.UserId,
                TokenHash = RefreshTokenHelper.HashToken(refreshToken),
                ExpiresAt = refreshExpiry,
                RevokedAt = null
            };
            _context.RefreshTokens.Add(refreshTokenEntity);

            await _context.SaveChangesAsync();

            return new IssuedTokens(accessToken, DateTime.UtcNow.AddMinutes(accessMinutes), refreshToken, refreshExpiry);
        }
    }
}
