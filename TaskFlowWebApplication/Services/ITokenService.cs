using TaskFlowWebApplication.Models;

namespace TaskFlowWebApplication.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user, IEnumerable<string> roles);
        string GenerateRefreshToken();
        DateTime GetAccessTokenExpiration();
    }
}
