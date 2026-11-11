using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class TwoFactorLoginDTO
    {
        [Required]
        public string ChallengeToken { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{6}$")]
        public string Code { get; set; } = string.Empty;
    }
}
