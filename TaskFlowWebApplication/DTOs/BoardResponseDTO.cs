namespace TaskFlowWebApplication.DTOs
{
    public class BoardResponseDTO
    {
        public int BoardId { get; set; }
        public int WorkspaceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? MyRole { get; set; }
    }
}
