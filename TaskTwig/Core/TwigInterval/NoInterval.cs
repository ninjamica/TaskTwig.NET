using System;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

public class NoInterval : ITwigInterval
{
    [JsonIgnore]
    public DateOnly? NextOccurrence => null;
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => null;
}