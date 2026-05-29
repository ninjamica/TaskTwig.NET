using System;

namespace TaskTwig.Core;

public record Sleep
{
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
}
