using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class Role
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
