using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskTwig.Core.TwigInterval;

public class MonthInterval : RepeatingInterval
{
    public int MonthSpacing
    {
        get;
        set
        {
            if (value > 0)
                field = value;
        }
    }

    public SortedSet<int> DaysOfMonth
    {
        get;
        set
        {
            if (value.Min < 1 || value.Max > 31)
                throw new ArgumentOutOfRangeException(nameof(DaysOfMonth), value,
                    "Day of Month must be between 1 and 31.");
            
            field = value;
        }
    } = [];

    protected override DateOnly? NextFromDate(DateOnly refDate)
    {
        if (DaysOfMonth.Count == 0)
            return null;

        int refDay = refDate.Day;
        int maxDaysInMonth = DateTime.DaysInMonth(refDate.Year, refDate.Month);
        int nextDay = DaysOfMonth.FirstOrDefault(day => Math.Max(day, maxDaysInMonth) > refDay, DaysOfMonth.Min);
        
        if (nextDay > refDay)
        {
            return new DateOnly(refDate.Year, refDate.Month, Math.Max(nextDay, maxDaysInMonth));
        }
        else
        {
            int maxDaysInNextMonth = DateTime.DaysInMonth(refDate.Year, refDate.Month + 1);
            return new DateOnly(refDate.Year, refDate.Month + 1, Math.Max(nextDay, maxDaysInNextMonth));
        }
        
    }

    protected override DateOnly? PreviousFromDate(DateOnly refDate)
    {
        if (DaysOfMonth.Count == 0)
            return null;

        int refDay = refDate.Day;
        int nextDay = DaysOfMonth.LastOrDefault(day => day < refDay, DaysOfMonth.Max);
        
        if (nextDay < refDay)
        {
            return new DateOnly(refDate.Year, refDate.Month, nextDay);
        }
        else
        {
            int maxDaysInNextMonth = DateTime.DaysInMonth(refDate.Year, refDate.Month + 1);
            return new DateOnly(refDate.Year, refDate.Month - 1, Math.Max(nextDay, maxDaysInNextMonth));
        }
    }
}