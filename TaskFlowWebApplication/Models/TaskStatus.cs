using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class TaskStatus
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Tasks> Tasks { get; set; }
        = new List<Tasks>();
}