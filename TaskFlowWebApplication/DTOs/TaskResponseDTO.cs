namespace TaskFlowWebApplication.DTOs
{
    public class TaskResponseDTO
    {
        public int Id { get; set; }

        public int ListId { get; set; }

        public string ListName { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public int StatusId { get; set; }

        public string StatusName { get; set; } = string.Empty;

        public int PriorityId { get; set; }

        public string PriorityName { get; set; } = string.Empty;

        public int CreatedBy { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int Position { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<TaskAssigneeResponseDTO> Assignees { get; set; } = new();
    }

    public class TaskAssigneeResponseDTO
    {
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;
    }
}
