using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core.TwigInterval;

public partial class SingleDateInterval: ObservableObject, ITwigInterval
{
    [JsonIgnore]
    public DateOnly? NextOccurrence => Date;
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => null;

    [ObservableProperty]
    public required partial DateOnly Date { get; set; }
}