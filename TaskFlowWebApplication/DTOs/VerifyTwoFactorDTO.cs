using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class VerifyTwoFactorDTO
    {
        [Required]
        [RegularExpression(@"^\d{6}$",
            ErrorMessage = "OTP phải gồm đúng 6 chữ số.")]
        public string Code { get; set; } = string.Empty;
    }
}
