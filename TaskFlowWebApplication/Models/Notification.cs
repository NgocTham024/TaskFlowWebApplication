using System;
using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Models;

public partial class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int? ActorId { get; set; }

    public int? TaskId { get; set; }

    public int? BoardId { get; set; }

    public string Type { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Users? Actor { get; set; }

    public virtual Board? Board { get; set; }

    public virtual Tasks? Task { get; set; }

    public virtual Users User { get; set; } = null!;
}