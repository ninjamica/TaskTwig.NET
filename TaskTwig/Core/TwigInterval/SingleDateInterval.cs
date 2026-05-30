using System;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

public class SingleDateInterval(DateOnly date) : ITwigInterval
{
    [JsonIgnore]
    public DateOnly? NextOccurrence => Date;
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => null;

    public DateOnly Date { get; set; } = date;
}