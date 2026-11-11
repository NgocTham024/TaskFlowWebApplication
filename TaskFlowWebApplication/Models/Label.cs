using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class Label
{
    public int Id { get; set; }

    public int WorkspaceId { get; set; }

    public string Name { get; set; } = null!;

    public string Color { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Workspace Workspace { get; set; } = null!;

    public virtual ICollection<Tasks> Tasks { get; set; }
        = new List<Tasks>();
}