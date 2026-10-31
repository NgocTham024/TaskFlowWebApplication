using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class AdminUpdateUserDTO
    {
        [MaxLength(100, ErrorMessage = "Username tối đa 100 ký tự")]
        public string? Username { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        public bool? IsEmailVerified { get; set; }
    }
}
