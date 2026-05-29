using System;

namespace TaskTwig.Core;

public record Task() : ITask
{
    public required string Name { get; set; }
    public DateOnly? LastDone { get; set; }
    
    public TaskCategory? Category { get; set; }
    public required int Points { get; set; }
}