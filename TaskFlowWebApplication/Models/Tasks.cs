namespace TaskFlowWebApplication.Models
{
    public class Tasks
    {
        public int Id { get; set; }

        public int ListId { get; set; }

        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        public int StatusId { get; set; }

        public int PriorityId { get; set; }

        public int CreatedBy { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? DueDate { get; set; }

        public DateTime? CompletedAt { get; set; }

        public int Position { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

        public virtual User CreatedByNavigation { get; set; } = null!;

        public virtual TaskList List { get; set; } = null!;

        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public virtual TaskPriority Priority { get; set; } = null!;

        public virtual TaskStatus Status { get; set; } = null!;

        public virtual ICollection<TaskAssignee> TaskAssignees { get; set; } = new List<TaskAssignee>();

        public virtual ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();

        public virtual ICollection<Label> Labels { get; set; } = new List<Label>();
    }
}
