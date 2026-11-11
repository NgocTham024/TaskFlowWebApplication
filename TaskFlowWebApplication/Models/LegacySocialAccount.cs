using System;
using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Models;

public partial class LegacySocialAccount
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Users User { get; set; } = null!;
}