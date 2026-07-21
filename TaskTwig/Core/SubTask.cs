using System;
using System.IO.Hashing;
using System.Text;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class SubTask : HashableObject
{
    [JsonIgnore]
    public TwTask? ParentTask { get; init; }
    
    [ObservableProperty]
    public required partial string Name { get; set; }

    // TODO: make observable (listen to external changes from parent)
    [JsonIgnore]
    public bool IsDone
    {
        get => ParentTask?._IsDone(LastDone) ?? false;
        set => LastDone = value ? TaskTwig.Today : null;
    }
    
    [ObservableProperty]
    private partial DateOnly? LastDone { get; set; }

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Name));
        
        if (LastDone is { } lastDone)
            hashAlgorithm.Append(BitConverter.GetBytes(lastDone.DayNumber));
    }
}