using System;

namespace TaskTwig.Core.TwigInterval;

public class DailyInterval : ITwigInterval
{
    public DateOnly? NextOccurrence => TaskTwig.Today;
    public DateOnly? PreviousOccurrence => TaskTwig.Today.AddDays(-1);
}