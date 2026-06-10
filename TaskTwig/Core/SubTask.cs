using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class SubTask : ObservableObject
{
    [JsonIgnore]
    public Task ParentTask { get; init; }
    
    [ObservableProperty]
    public required partial string Name { get; set; }

    // TODO: make observable (listen to external changes from parent)
    [JsonIgnore]
    public bool IsDone
    {
        get => ParentTask._IsDone(LastDone);
        set => LastDone = value ? TaskTwig.Today : null;
    }
    
    [ObservableProperty]
    private partial DateOnly? LastDone { get; set; }
}