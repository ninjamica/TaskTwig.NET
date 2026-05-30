using System;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

[JsonConverter(typeof(JsonStringEnumConverter))]
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
    public DateUnit Unit { get; set; } = DateUnit.Day;

    protected override DateOnly? NextFromDate(DateOnly refDate)
    {
        return Unit switch
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
        return Unit switch
        {
            DateUnit.Day => refDate.AddDays(-UnitAmount),
            DateUnit.Week => refDate.AddDays(-UnitAmount * 7),
            DateUnit.Month => refDate.AddMonths(-UnitAmount),
            DateUnit.Year => refDate.AddYears(-UnitAmount),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}