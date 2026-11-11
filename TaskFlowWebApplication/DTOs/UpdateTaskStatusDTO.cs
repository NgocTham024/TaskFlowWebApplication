using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class UpdateTaskStatusDTO
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int StatusId { get; set; }
    }
}
