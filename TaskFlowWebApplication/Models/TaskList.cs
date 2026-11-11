using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class TaskList
{
    public int Id { get; set; }

    public int ColumnId { get; set; }

    public string Name { get; set; } = null!;

    public int Position { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual BoardColumn Column { get; set; } = null!;

    public virtual ICollection<Tasks> Tasks { get; set; }
        = new List<Tasks>();
}
