using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class MoveTaskDTO
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int TargetListId { get; set; }
    }
}
