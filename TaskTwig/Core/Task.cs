using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.Core;

public class Task() : ITask
{
    public enum OccurrencePattern
    {
        OccurOn,
        DueBy,
        StartOn
    }

    public enum ExtendPattern
    {
        NoExtend,
        OnCompletion,
        FromCompletion,
        Auto
    }
    
    public required string Name { get; set; }
    public bool IsDone { get => _IsDone(LastDone); set => _SetDone(value); }
    public bool IsToday => _IsToday();

    [JsonIgnore]
    public TaskCategory? Category { get; set; }
    public required ITwigInterval Interval { get; set; }
    public int Points { get; set; } = 1;
    public OccurrencePattern OPattern { get; set; }
    public ExtendPattern EPattern { get; set; }
    
    private DateOnly? LastDone { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public List<SubTask> SubTasks { get; init; } = [];

    internal bool _IsDone(DateOnly? lastDone)
    {
        if (lastDone is null)
            return false;

        if (Interval.NextOccurrence is null || Interval.PreviousOccurrence is null)
            return true;
        
        return lastDone.Value.DayNumber > Interval.PreviousOccurrence.Value.DayNumber;
    }

    private void _SetDone(bool done)
    {
        LastDone = done ? TaskTwig.Today : null;
    }

    internal bool _IsToday()
    {
        if (IsDone)
            return false;

        return Interval.NextOccurrence is null;
    }
}