using System;
using System.Collections.Generic;

namespace TaskFlowWebApplication.Models;

public partial class BoardColumn
{
    public int Id { get; set; }

    public int BoardId { get; set; }

    public string Name { get; set; } = null!;

    public int Position { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Board Board { get; set; } = null!;

    public virtual ICollection<TaskList> TaskLists { get; set; }
        = new List<TaskList>();
}