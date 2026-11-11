using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class ReorderTasksDTO
    {
        [Required]
        [MinLength(1)]
        public List<int> TaskIds { get; set; } = new();
    }
}
