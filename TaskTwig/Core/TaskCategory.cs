using System.Collections.Generic;
using Avalonia.Media;

namespace TaskTwig.Core;

public record TaskCategory
{
    public required string Name { get; set; }
    public required Color Color { get; set; }
    public List<Task> Tasks { get; } = [];

    public void AddTask(Task task)
    {
        if (task.Category is not null)
        {
            task.Category.Tasks.Remove(task);
        }
        Tasks.Add(task);
        task.Category = this;
    }
}