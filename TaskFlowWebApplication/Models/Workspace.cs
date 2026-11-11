using System;
using System.Collections.Generic;
using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Models;

public partial class Workspace
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<ActivityLog> ActivityLogs { get; set; }
        = new List<ActivityLog>();

    public virtual ICollection<Board> Boards { get; set; }
        = new List<Board>();

    public virtual Users CreatedByNavigation { get; set; } = null!;

    public virtual Label? Label { get; set; }

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; }
        = new List<WorkspaceMember>();
}