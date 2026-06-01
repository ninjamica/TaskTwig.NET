using System;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core.TwigInterval;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DateUnit
{
    Day,
    Week,
    Month,
    Year
}

public partial class UnitInterval : RepeatingInterval
{

    [ObservableProperty]
    public required partial int UnitAmount { get; set; }

    [ObservableProperty]
    public required partial DateUnit Unit { get; set; }

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