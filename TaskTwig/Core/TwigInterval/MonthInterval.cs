using System;
using System.Numerics;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core.TwigInterval;

public partial class MonthInterval : RepeatingInterval
{
    [ObservableProperty] public partial int MonthSpacing { get; set; } = 1;
    [ObservableProperty] public partial uint DaysOfMonthMap { get; set; } = 0u;

    protected override DateOnly? NextFromDate(DateOnly refDate)
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

    protected override DateOnly? PreviousFromDate(DateOnly refDate)
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

    public bool IsOnDay(int day)
    {
        return (DaysOfMonthMap & (1 << (day - 1)) & 0x7FFF_FFFF) != 0;
    }
    
    partial void OnMonthSpacingChanged(int value) => UpdateOccurrences();
    partial void OnDaysOfMonthMapChanged(uint value) => UpdateOccurrences();
}