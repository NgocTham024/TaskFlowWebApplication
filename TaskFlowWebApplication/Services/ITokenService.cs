using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(Users user, IEnumerable<string> roles);
        string GenerateRefreshToken();
        DateTime GetAccessTokenExpiration();
    }
}
