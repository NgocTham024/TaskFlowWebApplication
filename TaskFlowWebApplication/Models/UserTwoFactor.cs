using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class UserTwoFactor
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string? SecretKeyProtected { get; set; }

    public string? PendingSecretKeyProtected { get; set; }

    public byte IsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? EnabledAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? LastUsedTotpStep { get; set; }

    public virtual User User { get; set; } = null!;
}
