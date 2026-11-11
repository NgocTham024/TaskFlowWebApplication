using System.ComponentModel.DataAnnotations;

namespace TaskFlowWebApplication.DTOs
{
    public class TaskListResponseDTO
    {
        public int Id { get; set; }
        public int ColumnId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Position { get; set; }
    }
    public class MoveTaskToColumnDTO
    {
        [Range(1, int.MaxValue)]
        public int TargetColumnId { get; set; }

        [Range(1, int.MaxValue)]
        public int? TargetListId { get; set; }
    }
    public class SaveTaskCommentDTO
    {
        [Required]
        public string Content { get; set; } = string.Empty;
    }
    public class TaskCommentResponseDTO
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    public class TaskLabelResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }
}
