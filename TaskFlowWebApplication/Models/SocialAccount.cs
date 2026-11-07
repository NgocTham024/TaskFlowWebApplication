using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class SocialAccount
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderUserId { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
