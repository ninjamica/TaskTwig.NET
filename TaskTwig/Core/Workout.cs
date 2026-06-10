using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace TaskTwig.Core;

public record Workout
{
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public Dictionary<Exercise, int> Exercises { get; init; } = [];
    
    [JsonIgnore]
    public TimeSpan Length => EndTime.Subtract(StartTime);
}