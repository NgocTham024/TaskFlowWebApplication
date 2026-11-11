using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class TaskAssignee
{
    public int TaskId { get; set; }

    public int UserId { get; set; }

    public int AssignedBy { get; set; }

    public DateTime AssignedAt { get; set; }

    public virtual User AssignedByNavigation { get; set; } = null!;

    public virtual Tasks Task { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
