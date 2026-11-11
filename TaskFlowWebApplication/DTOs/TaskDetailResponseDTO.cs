namespace TaskFlowWebApplication.DTOs
{
    public class TaskDetailResponseDTO : TaskResponseDTO
    {
        public List<TaskCommentResponseDTO> Comments { get; set; } = new List<TaskCommentResponseDTO>();
        public int ColumnId { get; set; }
        public string ColumnName { get; set; } = string.Empty;
        public int BoardId { get; set; }
        public string BoardName { get; set; } = string.Empty;
    }
}
