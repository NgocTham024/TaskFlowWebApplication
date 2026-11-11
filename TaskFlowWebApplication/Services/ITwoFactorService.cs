using TaskFlowWebApplication.DTOs;

namespace TaskFlowWebApplication.Services
{
    public interface ITwoFactorService
    {
        Task<TwoFactorSetupResponseDTO> SetupAsync(int userId);

        Task<bool> VerifySetupAsync(int userId, string code);

        Task<bool> DisableAsync(
            int userId,
            string password,
            string code);

        Task<string> CreateLoginChallengeAsync(int userId);

        Task<int?> VerifyLoginAsync(
            string challengeToken,
            string code);
    }
}
