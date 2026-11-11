using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class AssignTaskDTO
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }
    }
}
