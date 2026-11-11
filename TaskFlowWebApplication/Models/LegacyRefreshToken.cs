using System;
using TaskFlowWebApplication.Model;

namespace TaskFlowWebApplication.Models;

public partial class LegacyRefreshToken
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public byte IsRevoked { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public virtual Users User { get; set; } = null!;
}