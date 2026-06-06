using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace TaskTwig.Core;

public readonly record struct Sleep
{
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    
    [SetsRequiredMembers]
    public Sleep(DateTime startTime, DateTime endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
    
    [JsonIgnore]
    public TimeSpan Length => EndTime.Subtract(StartTime);

    [JsonIgnore]
    public DateOnly Date => DateOnly.FromDateTime(EndTime).AddDays(-1);
}
