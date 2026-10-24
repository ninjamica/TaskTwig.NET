using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Threading;
using WeakEvent;

namespace TaskTwig.Core;

public class TodayChangedEventArgs(DateOnly prevDate, DateOnly currentDate) : EventArgs
{
    public DateOnly CurrentDate { get; } = currentDate;
    public DateOnly PreviousDate { get; } = prevDate;
}

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
            Console.WriteLine($"DayStart={value}, Today={Today}");
            SetRefreshTimer();
        }
    } = new(5, 0, 0);

    /// <summary>
    /// The time at which the evening starts, used to determine when it's day vs night.
    /// </summary>
    public static TimeSpan NightStart { get; set; } = new(18, 0, 0);

    /// <summary>
    /// The current effective date. If the current time is after midnight but before <see cref="DayStart"/>,
    /// the value will be of the day before.
    /// </summary>
    public static DateOnly Today
    {
        get;
        private set
        {
            var prevVal = field;
            field = value;
            
            if (prevVal != value)
                TodayChangedEventSource.Raise(null, new TodayChangedEventArgs(prevVal, value));
        }
    } = EffectiveDate(DateTime.Now);

    /// <summary>
    /// Calculates the effective date of a timestamp (where the day only starts on <see cref="DayStart"/>).
    /// </summary>
    /// <param name="dateTime">Timestamp to calculate date from</param>
    /// <returns>The effective date of the provided timestamp</returns>
    public static DateOnly EffectiveDate(DateTime dateTime)
    {
        DateOnly date = DateOnly.FromDateTime(dateTime);
        
        if (dateTime.TimeOfDay.CompareTo(DayStart) < 0)
            date = date.AddDays(-1);
        
        return date;
    }

    /// <summary>
    /// Calculate whether the current time (<see cref="DateTime.Now"/>) is during the day or night.
    /// See <see cref="IsDaytime(TimeSpan)"/>
    /// </summary>
    /// <returns>true if it is currently daytime.</returns>
    public static bool IsDaytime()
    {
        return IsDaytime(DateTime.Now.TimeOfDay);
    }

    /// <summary>
    /// Calculates whether the provided time is during the day (on/after <see cref="DayStart"/> and before
    /// <see cref="NightStart"/>) or the night.
    /// </summary>
    /// <param name="time">The time of day to check.</param>
    /// <returns>true if the provided time is during the day.</returns>
    public static bool IsDaytime(TimeSpan time)
    {
        return time >= DayStart && time < NightStart;
    }

    /// <summary>
    /// Recalculates <see cref="Today"/> based on the current time (<see cref="DateTime.Now"/>). Also starts a timer
    /// that 
    /// </summary>
    public static void RefreshToday()
    {
        Today = EffectiveDate(DateTime.Now);
        SetRefreshTimer();
    }
    
    public static event EventHandler<TodayChangedEventArgs> OnTodayChanged
    {
        add => TodayChangedEventSource.Subscribe(value);
        remove => TodayChangedEventSource.Unsubscribe(value);
    }
    
    public static IObservable<EventPattern<TodayChangedEventArgs>> TodayChangedReevaluator =>
        Observable.FromEventPattern<TodayChangedEventArgs>(handler => OnTodayChanged += handler, 
            handler => OnTodayChanged -= handler);
    
    
    static TwigTime()
    {
        RefreshTimer.Tick += OnRefreshTimerTick;
        SetRefreshTimer();
    }
    
    private static readonly DispatcherTimer RefreshTimer = new(DispatcherPriority.Background, Dispatcher.UIThread);
    private static readonly WeakEventSource<TodayChangedEventArgs> TodayChangedEventSource = new();
    
    private static void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        RefreshTimer.Stop();
        RefreshToday();
        
        Console.WriteLine($"Refreshing Today: Today={Today}");
    }

    private static void SetRefreshTimer()
    {
        RefreshTimer.Stop();
        
        var now = DateTime.Now.TimeOfDay;
        RefreshTimer.Interval = DayStart - now + (now < DayStart ? TimeSpan.FromSeconds(5) : new TimeSpan(1, 0, 0, 5));
        Console.WriteLine($"Setting Today Refresh Timer: Interval={RefreshTimer.Interval}");
        
        RefreshTimer.Start();
    }
}