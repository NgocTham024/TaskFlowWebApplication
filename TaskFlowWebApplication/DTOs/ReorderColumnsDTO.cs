using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class ReorderColumnsDTO
    {
        [Required]
        public List<int> ColumnIds { get; set; } = new();
    }
}
