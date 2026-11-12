using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class AssignRoleDTO
    {
        [Required(ErrorMessage = "RoleId là bắt buộc")]
        public int RoleId { get; set; }
    }
}
