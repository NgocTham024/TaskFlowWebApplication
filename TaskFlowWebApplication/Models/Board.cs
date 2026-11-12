using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class Board
{
    public int Id { get; set; }

    public int WorkspaceId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; } = new List<ActivityLog>();

    public virtual ICollection<BoardColumn> BoardColumns { get; set; } = new List<BoardColumn>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Workspace Workspace { get; set; } = null!;
}
