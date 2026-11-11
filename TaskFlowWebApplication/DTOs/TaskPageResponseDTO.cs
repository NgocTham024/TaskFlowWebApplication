namespace TaskFlowWebApplication.DTOs
{
    public class TaskPageResponseDTO
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public List<TaskResponseDTO> Items { get; set; } = new();
    }
}
