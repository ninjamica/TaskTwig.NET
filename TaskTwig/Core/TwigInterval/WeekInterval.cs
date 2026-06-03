using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core.TwigInterval;

[Flags]
public enum DayOfWeekFlag
{
    None = 0,
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64
}

public partial class WeekInterval : RepeatingInterval
{
    [ObservableProperty]
    public required partial int WeekSpacing { get; set; }

    // {
    //     get;
    //     set
    //     {
    //         if (value > 0)
    //             field = value;
    //     }
    // } = 1;

    [ObservableProperty]
    public partial DayOfWeekFlag DayOfWeekMap { get; set; } = DayOfWeekFlag.None;

    protected override DateOnly? NextFromDate(DateOnly refDate)
    {
        int refDay = (int) refDate.DayOfWeek;
        int nextDay = _getNextDayOfWeek(refDay);

        if (nextDay == -1)
            return null;
        
        DateOnly nextDate = refDate.AddDays(nextDay - refDay);
        if (nextDay <= refDay)
            nextDate = nextDate.AddDays(7 * WeekSpacing);

        return nextDate;
    }

    protected override DateOnly? PreviousFromDate(DateOnly refDate)
    {
        int refDay = (int) refDate.DayOfWeek;
        int prevDay = _getPrevDayOfWeek(refDay);
        
        if (prevDay == -1)
            return null;
        
        DateOnly prevDate = refDate.AddDays(-(prevDay - refDay));
        if (prevDay >= refDay)
            prevDate = prevDate.AddDays(-7 * WeekSpacing);
        
        return prevDate;
    }

    private int _getNextDayOfWeek(int day)
    {
        for (int dayPlus = 1; dayPlus <= 7; dayPlus++) {
            int nextDay = (day + dayPlus) % 7;
            if (DayOfWeekMap.HasFlag((DayOfWeekFlag)(1 << nextDay)))
                return nextDay;
        }
        return -1;
    }

    private int _getPrevDayOfWeek(int day)
    {
        for (int dayMinus = 1; dayMinus <= 7; dayMinus++) {
            int prevDay = (day - dayMinus + 7) % 7;
            if (DayOfWeekMap.HasFlag((DayOfWeekFlag)(1 << prevDay)))
                return prevDay;
        }
        return -1;
    }

    partial void OnWeekSpacingChanged(int value) => UpdateOccurrences();
    partial void OnDayOfWeekMapChanged(DayOfWeekFlag value) => UpdateOccurrences();
}