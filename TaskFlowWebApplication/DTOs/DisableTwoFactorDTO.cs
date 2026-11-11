using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class DisableTwoFactorDTO
    {
        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\d{6}$",
            ErrorMessage = "OTP phải gồm đúng 6 chữ số.")]
        public string Code { get; set; } = string.Empty;
    }
}
