using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class MoveTaskDTO
    {
        [Range(1, int.MaxValue)]
        public int TargetColumnId { get; set; }
        [Range(1, int.MaxValue)]
        public int? TargetListId { get; set; }
    }
}
