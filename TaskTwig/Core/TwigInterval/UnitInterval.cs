using System;
using System.ComponentModel.DataAnnotations;
using System.IO.Hashing;
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
    [WatchForHash]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, int.MaxValue, ErrorMessage = "Must occur at least every 1 unit.")]
    public partial int UnitAmount { get; set; } = 1;

    [WatchForHash]
    [ObservableProperty] 
    public partial DateUnit Unit { get; set; }

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

    protected override void _AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("UnitInterval"u8);
        hashAlgorithm.Append(BitConverter.GetBytes(UnitAmount));
        hashAlgorithm.Append(BitConverter.GetBytes((int)Unit));
    }

    partial void OnUnitAmountChanged(int value) => UpdateOccurrences();
    partial void OnUnitChanged(DateUnit value) => UpdateOccurrences();
}