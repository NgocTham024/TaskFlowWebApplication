using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class ResendVerificationDTO
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
