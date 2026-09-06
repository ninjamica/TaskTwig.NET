using System;
using System.ComponentModel;
using WeakEvent;

namespace TaskTwig.Core;

public static class TwigTime
{
    /// <summary>
    /// The time at which the next day starts, it must be on or after midnight and should be in the morning.
    /// </summary>
    public static TimeSpan DayStart
    {
        get;
        set
        {
            field = value;
            Today = EffectiveDate(DateTime.Now);
        }
    } = new(5, 0, 0);

    /// <summary>
    /// The current effective date. If the current time is after midnight but before <c>DayStart</c>,
    /// the value will be of the day before.
    /// </summary>
    public static DateOnly Today
    {
        get;
        private set
        {
            field = value;
            TodayChangedEventSource.Raise(null, new PropertyChangedEventArgs(nameof(Today)));
        }
    } = EffectiveDate(DateTime.Now);

    /// <summary>
    /// Calculates the effective date of a timestamp (where the day only starts after <c>DayStart</c>).
    /// </summary>
    /// <param name="dateTime">Timestamp to calculate date from</param>
    /// <returns></returns>
    public static DateOnly EffectiveDate(DateTime dateTime)
    {
        DateOnly date = DateOnly.FromDateTime(dateTime);
        
        if (dateTime.TimeOfDay.CompareTo(DayStart) < 0)
            date = date.AddDays(-1);
        
        return date;
    }

    private static readonly WeakEventSource<PropertyChangedEventArgs> TodayChangedEventSource = new();
    public static event EventHandler<PropertyChangedEventArgs> OnTodayChanged
    {
        add => TodayChangedEventSource.Subscribe(value);
        remove => TodayChangedEventSource.Unsubscribe(value);
    }

    public static void RefreshToday()
    {
        Today = EffectiveDate(DateTime.Now);
    }
}