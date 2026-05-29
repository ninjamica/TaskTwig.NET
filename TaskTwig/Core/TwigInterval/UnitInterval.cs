using System;

namespace TaskTwig.Core.TwigInterval;

public enum DateUnit
{
    Day,
    Week,
    Month,
    Year
}

public class UnitInterval : RepeatingInterval
{
    
    public int UnitAmount { get; set; } = 1;
    public DateUnit UnitType { get; set; } = DateUnit.Day;

    protected override DateOnly? NextFromDate(DateOnly refDate)
    {
        return UnitType switch
        {
            DateUnit.Day => refDate.AddDays(UnitAmount),
            DateUnit.Week => refDate.AddDays(UnitAmount * 7),
            DateUnit.Month => refDate.AddMonths(UnitAmount),
            DateUnit.Year => refDate.AddYears(UnitAmount),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    protected override DateOnly? PreviousFromDate(DateOnly refDate)
    {
        return UnitType switch
        {
            DateUnit.Day => refDate.AddDays(-UnitAmount),
            DateUnit.Week => refDate.AddDays(-UnitAmount * 7),
            DateUnit.Month => refDate.AddMonths(-UnitAmount),
            DateUnit.Year => refDate.AddYears(-UnitAmount),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}