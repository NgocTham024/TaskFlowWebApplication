namespace TaskFlowWebApplication.Services
{
    public interface IAuthService
    {
        Task<(
            bool Success,
            string Message,
            string? AccessToken,
            DateTime? ExpiresAt,
            string? RefreshToken,
            int RefreshTokenExpirationDays
        )> RefreshTokenAsync(string refreshToken);
    }
}
