using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class CreateTaskDTO
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        [Required]
        [Range(1, int.MaxValue)]
        public int PriorityId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
    }
}
