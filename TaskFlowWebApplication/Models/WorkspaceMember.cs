using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class WorkspaceMember
{
    public int Id { get; set; }

    public int WorkspaceId { get; set; }

    public int UserId { get; set; }

    public int RoleId { get; set; }

    public DateTime JoinedAt { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Workspace Workspace { get; set; } = null!;
}
