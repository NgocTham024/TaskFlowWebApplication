
using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class GoogleLoginDTO
    {
        [Required]
        public string IdToken { get; set; } = string.Empty;
    }
}
