using System;

namespace TaskTwig.Core.TwigInterval;

public class SingleDateInterval(DateOnly date) : ITwigInterval
{
    public DateOnly? NextOccurrence => Date;
    public DateOnly? PreviousOccurrence => null;

    public DateOnly Date { get; set; } = date;
}