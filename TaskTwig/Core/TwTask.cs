using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AutoExtendPattern
{
    NoExtend,
    OnCompletion,
    FromCompletion,
    Auto
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OccurrencePattern
{
    DueBy,
    OccurOn,
    StartOn
}

public partial class TwTask : HashableObject
{
    [ObservableProperty]
    public required partial string Name { get; set; }
    
    [JsonIgnore]
    public TaskCategory Category { get; set; }
    
    [ObservableProperty]
    public partial int Points { get; set; } = 1;

    [ObservableProperty]
    public required partial ITwigInterval Interval { get; set; }
    partial void OnIntervalChanged(ITwigInterval value)
    {
        _UpdateOPattern(OPattern);
        _UpdateEPattern(EPattern);
        _UpdateStatusVars();
    }
    
    [ObservableProperty]
    public partial OccurrencePattern OPattern { get; set; }
    partial void OnOPatternChanged(OccurrencePattern value)
    {
        _UpdateOPattern(value);
        _UpdateStatusVars();
    }
    
    [ObservableProperty]
    public partial AutoExtendPattern EPattern { get; set; }
    partial void OnEPatternChanged(AutoExtendPattern value)
    {
        _UpdateEPattern(value);
        _UpdateStatusVars();
    }

    [ObservableProperty]
    public partial DateOnly? LastDone { get; set; }

    partial void OnLastDoneChanged(DateOnly? value) => _UpdateStatusVars();
    
    public ObservableCollection<SubTask> SubTasks { get; init; } = [];
    
    
    // TODO: Initialize
    [JsonIgnore]
    [ObservableProperty]
    public partial bool IsDone { get; private set; }
    
    // TODO: Initialize
    [JsonIgnore]
    [ObservableProperty]
    public partial bool IsToday { get; private set; }
    
    // TODO: Initialize
    [JsonIgnore]
    [ObservableProperty]
    public partial bool IsOverdue { get; private set; }

    
    internal bool _IsDone(DateOnly? lastDone)
    {
        if (lastDone is null)
            return false;

        if (lastDone == TaskTwig.Today || Interval.PreviousOccurrence is null)
            return true;
        
        return lastDone.Value.CompareTo(Interval.PreviousOccurrence.Value) > 0;
    }
    
    public void SetDone(bool done)
    {
        LastDone = done ? TaskTwig.Today : null;

        if (Interval is RepeatingInterval repeatingInterval)
        {
            switch (EPattern)
            {
                case AutoExtendPattern.OnCompletion when done:
                {
                    if (repeatingInterval.NextFromToday() is { } nextFromToday)
                        repeatingInterval.ReferenceDate = nextFromToday;
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
        
        _UpdateStatusVars();
    }

    private bool _IsToday()
    {
        if (IsDone)
            return false;
        
        if (Interval is NoInterval or DailyInterval)
            return true;

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

    private void _UpdateStatusVars()
    {
        IsDone = _IsDone(LastDone);
        IsToday = _IsToday();
        IsOverdue = _IsOverdue();
    }

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Name));
        hashAlgorithm.Append(BitConverter.GetBytes(Points));
        // Interval.AppendHash(hashAlgorithm);
        hashAlgorithm.Append(BitConverter.GetBytes((int)OPattern));
        hashAlgorithm.Append(BitConverter.GetBytes((int)EPattern));
        
        if (LastDone is { } lastDone)
            hashAlgorithm.Append(BitConverter.GetBytes(lastDone.DayNumber));
        
        // foreach (var subTask in SubTasks)
        //     subTask.AppendHash(hashAlgorithm);
    }

    protected override void AppendHashableChildren(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        ((HashableObject)Interval).AppendHashAndChildren(mainHasher, childHasher);
        
        foreach (var subTask in SubTasks) 
            subTask.AppendHashAndChildren(mainHasher, childHasher);
    }
}