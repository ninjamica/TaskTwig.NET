using System;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

public class DailyInterval : ITwigInterval
{
    [JsonIgnore]
    public DateOnly? NextOccurrence => TaskTwig.Today;
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => TaskTwig.Today.AddDays(-1);
}