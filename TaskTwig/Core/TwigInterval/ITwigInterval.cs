using System;

namespace TaskTwig.Core.TwigInterval;

public interface ITwigInterval
{
    DateOnly? NextOccurrence { get; }
    DateOnly? PreviousOccurrence { get; }
}