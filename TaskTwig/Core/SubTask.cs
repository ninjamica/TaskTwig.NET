using System;
using System.Text.Json.Serialization;

namespace TaskTwig.Core;

public record SubTask() : ITask
{
    [JsonIgnore]
    public Task ParentTask { get; init; }
    public required string Name { get; set; }

    [JsonIgnore]
    public bool IsDone
    {
        get => ParentTask._IsDone(LastDone);
        set => LastDone = value ? TaskTwig.Today : null;
    }
    
    private DateOnly? LastDone { get; set; }
}