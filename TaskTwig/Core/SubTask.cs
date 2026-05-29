using System;

namespace TaskTwig.Core;

public record SubTask() : ITask
{
    public required string Name { get; set; }
    public DateOnly? LastDone { get; set; }
    public required Task ParentTask { get; init; }
}