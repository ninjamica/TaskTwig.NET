using System;
using System.ComponentModel.DataAnnotations;
using System.IO.Hashing;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core.TwigInterval;

public partial class MonthInterval : RepeatingInterval
{
    /// <summary>
    /// The number of months between occurrences. For example, a value of 1 means the interval occurs every month,
    /// a value of 2 means it occurs every other month, etc.
    /// </summary>
    [WatchForHash]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, int.MaxValue, ErrorMessage = "Must occur at least every 1 month.")]
    public partial int MonthSpacing { get; set; } = 1;
    
    /// <summary>
    /// Bitmap representing which days of the month the interval occurs on. The least significant 31 bits are used,
    /// where each bit represents whether the day is included (1=included), starting with the 1st of the month at the
    /// least significant bit where each following bit represents the next day of the month.
    /// 
    /// For months with less than 31 days, any day beyond that month's last day from this map counts for that month's
    /// last day. For example, April will register either the 31st or 30th as occuring on the 30th (April only has 30
    /// days).
    /// </summary>
    [WatchForHash]
    [ObservableProperty] 
    [NotifyDataErrorInfo]
    [Range(0, 0x7FFF_FFFF, ErrorMessage = "Invalid month date map, must be between {0} and {1}.")]
    public partial uint DaysOfMonthMap { get; set; } = 0u;
    

    /// <inheritdoc/>
    protected override DateOnly? NextAfterDate(DateOnly refDate)
    {
        if ((DaysOfMonthMap & 0x7FFF_FFFF) == 0)
            return null;
        
        int maxDaysInMonth = DateTime.DaysInMonth(refDate.Year, refDate.Month);
        DateOnly nextMonthDate = refDate.AddMonths(1);
        int maxDaysInNextMonth = DateTime.DaysInMonth(nextMonthDate.Year, nextMonthDate.Month);
        
        int refDay = refDate.Day;
        int nextDay = _FindNextDay(refDay, maxDaysInMonth, maxDaysInNextMonth);

        if (nextDay > refDay)
            return new DateOnly(refDate.Year, refDate.Month, nextDay);
        else
            return new DateOnly(nextMonthDate.Year, nextMonthDate.Month, nextDay);

    }

    /// <inheritdoc/>
    protected override DateOnly? PreviousBeforeDate(DateOnly refDate)
    {
        if ((DaysOfMonthMap & 0x7FFF_FFFF) == 0)
            return null;

        DateOnly prevMonthDate = refDate.AddMonths(-1);
        int maxDaysInPrevMonth = DateTime.DaysInMonth(prevMonthDate.Year, prevMonthDate.Month);
        
        int refDay = refDate.Day;
        int prevDay = _FindPrevDay(refDay, maxDaysInPrevMonth);

        if (prevDay < refDay)
            return new DateOnly(refDate.Year, refDate.Month, prevDay);
        else
            return new DateOnly(prevMonthDate.Year, prevMonthDate.Month, prevDay);
    }

    /// <inheritdoc/>
    protected override void AppendPartialHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("MonthInterval"u8);
        hashAlgorithm.Append(BitConverter.GetBytes(MonthSpacing));
        hashAlgorithm.Append(BitConverter.GetBytes(DaysOfMonthMap));
    }
    

    private int _FindNextDay(int day, int maxDaysInMonth, int maxDaysInNextMonth)
    {
        uint daysMap = DaysOfMonthMap & 0x7FFF_FFFF; 
        if (daysMap == 0)
            return 0;
        
        uint maskedMap = daysMap & ((1u << day) - 1u);
        return Math.Min(
            BitOperations.TrailingZeroCount((maskedMap == 0 || day >= maxDaysInMonth) ? daysMap : maskedMap) + 1, 
            maxDaysInNextMonth);
    }

    private int _FindPrevDay(int day, int maxDaysInPrevMonth)
    {
        uint daysMap = DaysOfMonthMap & 0x7FFF_FFFF; 
        if (daysMap == 0)
            return 0;
        
        uint maskedMap = daysMap & (~0u >> (33 - day));
        return Math.Min(32 - BitOperations.LeadingZeroCount(maskedMap == 0 ? daysMap : maskedMap), maxDaysInPrevMonth);
    }
    
    partial void OnMonthSpacingChanged(int value) => RecalcOccurrences();
    partial void OnDaysOfMonthMapChanged(uint value) => RecalcOccurrences();
}