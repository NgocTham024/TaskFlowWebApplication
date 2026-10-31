using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class CreateRoleDTO
    {
        [Required(ErrorMessage = "Tên role là bắt buộc")]
        [MaxLength(50, ErrorMessage = "Tên role tối đa 50 ký tự")]
        public string RoleName { get; set; } = string.Empty;
    }
}
