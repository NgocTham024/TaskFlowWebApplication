using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class CreateBoardDTO
    {
        [Required(ErrorMessage ="Tên bảng không được để trống.")]
        [StringLength(50, ErrorMessage = "Tên bảng tối đa 50 ký tự.")]
        public string Name { get; set; } = string.Empty;
        [StringLength(200, ErrorMessage = "Mô tả tối đa 100 ký tự. ")]
        public string? Description { get; set; }
    }
}
