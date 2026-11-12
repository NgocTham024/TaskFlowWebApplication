namespace TaskFlowWebApplication.DTOs
{
    public class TaskStatusResponseDTO: TaskResponseDTO
    {
        public int IdStatus { get; set; }
        public string Name { get; set; } = string.Empty;
        // mô tả trạng thái của task: todo, doing, done 
        public string? Description { get; set; }
    }
}
