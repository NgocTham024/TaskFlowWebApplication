namespace TaskFlowWebApplication.Services
{
    public interface ISocialAuthService
    {
        Task<SocialLoginResult> GoogleLoginAsync(string idToken);
    }

    public class SocialLoginResult
    {
        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public int? UserId { get; set; }

        public bool RequiresTwoFactor { get; set; }

        public string? ChallengeToken { get; set; }
    }
}
