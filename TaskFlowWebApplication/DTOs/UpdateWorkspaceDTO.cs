using System.ComponentModel.DataAnnotations;
namespace TaskFlowWebApplication.DTOs
{
    public class UpdateWorkspaceDTO
    {
        [Required(ErrorMessage = "Tên workspace không được để trống.")]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        [StringLength(200)]
        public string? Description { get; set; }
    }
}
