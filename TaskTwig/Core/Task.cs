using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.Core;

public partial class Task() : ObservableObject, ITask
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum OccurrencePattern
    {
        DueBy,
        OccurOn,
        StartOn
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AutoExtendPattern
    {
        NoExtend,
        OnCompletion,
        FromCompletion,
        Auto
    }
    
    [ObservableProperty]
    public required partial string Name { get; set; }
    
    [JsonIgnore]
    public TaskCategory? Category { get; set; }
    
    [ObservableProperty]
    public partial int Points { get; set; } = 1;

    // TODO: Make [ObservableProperty]
    public required ITwigInterval Interval
    {
        get;
        set
        {
            field = value;
            _UpdateOPattern(OPattern);
            _UpdateEPattern(EPattern);
        }
    }
    
    public OccurrencePattern OPattern
    {
        get;
        set
        {
            field = value;
            _UpdateOPattern(value);
        }
    }
    
    public AutoExtendPattern EPattern
    {
        get;
        set
        {
            field = value;
            _UpdateEPattern(value);
        }
    }

    [ObservableProperty]
    private partial DateOnly? LastDone { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public ObservableCollection<SubTask> SubTasks { get; init; } = [];
    
    
    // TODO: Make Observable
    [JsonIgnore]
    public bool IsDone { get => _IsDone(LastDone); set => _SetDone(value); }
    
    // TODO: Make Observable
    [JsonIgnore]
    public bool IsToday => _IsToday();
    
    // TODO: Make Observable
    [JsonIgnore]
    public bool IsOverdue => _IsOverdue();

    internal bool _IsDone(DateOnly? lastDone)
    {
        if (lastDone is null)
            return false;

        if (lastDone == TaskTwig.Today || Interval.PreviousOccurrence is null)
            return true;
        
        return lastDone.Value.CompareTo(Interval.PreviousOccurrence.Value) > 0;
    }

    private void _SetDone(bool done)
    {
        LastDone = done ? TaskTwig.Today : null;

        if (Interval is RepeatingInterval repeatingInterval)
        {
            switch (EPattern)
            {
                case AutoExtendPattern.OnCompletion when done:
                {
                    if (repeatingInterval.NextFromToday != null)
                        repeatingInterval.ReferenceDate = repeatingInterval.NextFromToday.Value;
                    break;
                }
                case AutoExtendPattern.FromCompletion or AutoExtendPattern.NoExtend when done:
                {
                    repeatingInterval.ReferenceDate = TaskTwig.Today;
                    break;
                }
                case AutoExtendPattern.OnCompletion or AutoExtendPattern.FromCompletion or AutoExtendPattern.NoExtend:
                {
                    if (repeatingInterval.PreviousOccurrence != null)
                        repeatingInterval.ReferenceDate = repeatingInterval.PreviousOccurrence.Value;
                    break;
                }
            }
        }
    }

    private bool _IsToday()
    {
        if (IsDone)
            return false;

        if (Interval.NextOccurrence is null)
            return false;

        return OPattern switch
        {
            OccurrencePattern.OccurOn => TaskTwig.Today.CompareTo(Interval.NextOccurrence.Value) == 0,
            OccurrencePattern.DueBy => TaskTwig.Today.CompareTo(Interval.NextOccurrence.Value) < 0,
            OccurrencePattern.StartOn => TaskTwig.Today.CompareTo(Interval.NextOccurrence.Value) >= 0,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    private bool _IsOverdue()
    {
        switch (OPattern)
        {
            case OccurrencePattern.StartOn or OccurrencePattern.OccurOn:
                return false;
            
            case OccurrencePattern.DueBy:
                if (IsDone || Interval.NextOccurrence is null)
                    return false;

                return TaskTwig.Today.CompareTo(Interval.NextOccurrence.Value) > 0;
                
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void _UpdateOPattern(OccurrencePattern pattern)
    {
        if (Interval is RepeatingInterval repeatingInterval)
        {
            repeatingInterval.RepeatTo = pattern switch
            {
                OccurrencePattern.OccurOn or OccurrencePattern.DueBy => RepeatPattern.OnAfter,
                OccurrencePattern.StartOn => RepeatPattern.OnBefore,
                _ => throw new ArgumentOutOfRangeException(nameof(pattern), pattern, null)
            };
        }
    }

    private void _UpdateEPattern(AutoExtendPattern pattern)
    {
        if (Interval is RepeatingInterval repeatingInterval)
        {
            repeatingInterval.AutoRepeat = pattern is AutoExtendPattern.Auto;
        }
    }
}