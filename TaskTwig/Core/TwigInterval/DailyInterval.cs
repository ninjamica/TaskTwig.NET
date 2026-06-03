using System;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

public class DailyInterval : ITwigInterval
{
    // TODO: Deal with TaskTwig.Today being possibly observable
    [JsonIgnore]
    public DateOnly? NextOccurrence => TaskTwig.Today;
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => TaskTwig.Today.AddDays(-1);
}