namespace TaskFlowWebApplication.DTOs
{
    public class ColumnResponseDTO
    {
        public int ColumnId { get; set; }

        public int BoardId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Position { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
