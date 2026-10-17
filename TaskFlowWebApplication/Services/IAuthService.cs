namespace TaskFlowWebApplication.Services
{
    public interface IAuthService
    {
        Task<(bool Success, string Message, string? AccessToken, DateTime? ExpiresAt)> RefreshTokenAsync(string refreshToken);
    }
}
