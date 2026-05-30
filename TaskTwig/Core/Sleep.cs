using System;

namespace TaskTwig.Core;

public readonly record struct Sleep
{
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
}
