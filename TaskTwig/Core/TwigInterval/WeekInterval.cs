using System;
using System.ComponentModel.DataAnnotations;
using System.IO.Hashing;
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
    /// <summary>
    /// The number of weeks between occurrences. For example, a value of 1 means the interval occurs every week,
    /// a value of 2 means it occurs every other week, etc.
    /// </summary>
    [WatchForHash]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, int.MaxValue, ErrorMessage = "Must occur at least every 1 week.")]
    public partial int WeekSpacing { get; set; } = 1;

    /// <summary>
    /// Flag enum that represents which days of the week the interval occurs on.
    /// </summary>
    [WatchForHash]
    [ObservableProperty]
    public partial DayOfWeekFlag DayOfWeekMap { get; set; } = DayOfWeekFlag.None;

    /// <inheritdoc/>
    protected override DateOnly? NextAfterDate(DateOnly refDate)
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

    /// <inheritdoc/>
    protected override DateOnly? PreviousBeforeDate(DateOnly refDate)
    {
        int refDay = (int) refDate.DayOfWeek;
        int prevDay = _getPrevDayOfWeek(refDay);
        
        if (prevDay == -1)
            return null;
        
        DateOnly prevDate = refDate.AddDays(prevDay - refDay);
        if (prevDay >= refDay)
            prevDate = prevDate.AddDays(-7 * WeekSpacing);
        
        return prevDate;
    }

    /// <inheritdoc/>
    protected override void AppendPartialHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("WeekInterval"u8);
        hashAlgorithm.Append(BitConverter.GetBytes(WeekSpacing));
        hashAlgorithm.Append(BitConverter.GetBytes((int)DayOfWeekMap));
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

    partial void OnWeekSpacingChanged(int value) => RecalcOccurrences();
    partial void OnDayOfWeekMapChanged(DayOfWeekFlag value) => RecalcOccurrences();
}