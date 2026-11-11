using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class SaveWorkspaceLabelDTO
    {
        [Required(ErrorMessage = "Tên nhãn không được để trống.")]
        [StringLength(20, ErrorMessage = "Tên nhãn không được dài hơn 20 ký tự.")]
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = "#808080";
    }
}
