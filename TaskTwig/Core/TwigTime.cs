using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Threading;
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
            Console.WriteLine($"DayStart={value}, Today={Today}");
            SetRefreshTimer();
        }
    } = new(5, 0, 0);

    /// <summary>
    /// The time at which the evening starts, used to determine when it's day vs night.
    /// </summary>
    public static TimeSpan NightStart { get; set; } = new(18, 0, 0);

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

    public static bool IsDaytime()
    {
        return IsDaytime(DateTime.Now.TimeOfDay);
    }

    public static bool IsDaytime(TimeSpan time)
    {
        return time >= DayStart && time < NightStart;
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
    
    static TwigTime()
    {
        RefreshTimer.Tick += OnRefreshTimerTick;
        SetRefreshTimer();
    }
    
    private static void OnRefreshTimerTick(object? sender, EventArgs e)
    {
        RefreshTimer.Stop();
        RefreshToday();
        SetRefreshTimer();
        
        Console.WriteLine($"Refreshing Today: Today={Today}");
    }
    
    private static readonly DispatcherTimer RefreshTimer = new(DispatcherPriority.Background, Dispatcher.UIThread);

    private static void SetRefreshTimer()
    {
        RefreshTimer.Stop();
        
        var now = DateTime.Now.TimeOfDay;
        RefreshTimer.Interval = now < DayStart ? DayStart - now : TimeSpan.FromDays(1) + DayStart - now;
        Console.WriteLine($"Setting Today Refresh Timer: Interval={RefreshTimer.Interval}");
        
        RefreshTimer.Start();
    }

    public static IObservable<EventPattern<PropertyChangedEventArgs>> TodayChangedReevaluator =>
        Observable.FromEventPattern<PropertyChangedEventArgs>(handler => OnTodayChanged += handler,
                                                              handler => OnTodayChanged -= handler);
}