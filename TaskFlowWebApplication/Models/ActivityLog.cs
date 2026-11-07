using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class ActivityLog
{
    public int Id { get; set; }

    public int WorkspaceId { get; set; }

    public int? BoardId { get; set; }

    public int? TaskId { get; set; }

    public int? UserId { get; set; }

    public string Action { get; set; } = null!;

    public string EntityType { get; set; } = null!;

    public int EntityId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Board? Board { get; set; }

    public virtual Tasks? Task { get; set; }

    public virtual User? User { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;
}
