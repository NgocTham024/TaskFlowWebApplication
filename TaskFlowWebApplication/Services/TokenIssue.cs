using System.Security.Cryptography;
using System.Text;
using TaskFlowWebApplication.Data;
using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Services
{
    public record IssuedTokens(string AccessToken, DateTime AccessTokenExpiration, string RefreshToken, DateTime RefreshTokenExpiration);
    public static class TokenHasher
    {
        public static string Hash(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
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

            var accessToken = _tokenService.GenerateAccessToken(user);
            var refreshToken = _tokenService.GenerateRefreshToken();
            var refreshExpiry = DateTime.UtcNow.AddDays(refreshDays);

            user.RefreshToken = TokenHasher.Hash(refreshToken);
            user.RefreshTokenExpiryTime = refreshExpiry;
            await _context.SaveChangesAsync();

            return new IssuedTokens(accessToken, DateTime.UtcNow.AddMinutes(accessMinutes), refreshToken, refreshExpiry);
        }
    }
}
