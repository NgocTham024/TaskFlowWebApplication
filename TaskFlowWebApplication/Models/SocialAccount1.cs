using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class SocialAccount1
{
    public int SocialAccountId { get; set; }

    public int UserId { get; set; }

    public string Provider { get; set; } = null!;

    public string ProviderUserId { get; set; } = null!;

    public string? ProviderEmail { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
