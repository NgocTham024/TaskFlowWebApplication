using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Services
{
    public interface ITokenService
    {
        string GenerateAccessToken(Users user);
        string GenerateRefreshToken();
        DateTime GetAccessTokenExpiration();
    }
}
