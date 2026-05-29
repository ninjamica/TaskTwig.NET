using System;

namespace TaskTwig.Core.TwigInterval;

public class NoInterval : ITwigInterval
{
    public DateOnly? NextOccurrence => null;
    public DateOnly? PreviousOccurrence => null;
}