using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class UpdateBoardDTO
    {
        [Required(ErrorMessage = "Tên workspace không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên workspace tối đa 100 ký tự.")]
        public string Name { get; set; } = string.Empty;
        [StringLength(200, ErrorMessage = "Mô tả tối đa 200 ký tự. ")]
        public string? Description { get; set; }
    }
}
