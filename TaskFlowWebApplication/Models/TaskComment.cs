using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class TaskComment
{
    public int Id { get; set; }

    public int TaskId { get; set; }

    public int UserId { get; set; }

    public string ContentCmt { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Tasks Task { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
