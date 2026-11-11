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
    /// <summary>
    /// The amount of units that pass between iterations of the interval (spacing between occurrences)
    /// </summary>
    [WatchForHash]
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(1, int.MaxValue, ErrorMessage = "Must occur at least every 1 unit.")]
    public partial int UnitAmount { get; set; } = 1;

    /// <summary>
    /// The unit which is used to calculate the spacing of the interval
    /// </summary>
    [WatchForHash]
    [ObservableProperty] 
    public partial DateUnit Unit { get; set; }

    
    /// <inheritdoc/>
    protected override DateOnly? NextAfterDate(DateOnly refDate)
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

    /// <inheritdoc/>
    protected override DateOnly? PreviousBeforeDate(DateOnly refDate)
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

    /// <inheritdoc/>
    protected override void AppendPartialHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("UnitInterval"u8);
        hashAlgorithm.Append(BitConverter.GetBytes(UnitAmount));
        hashAlgorithm.Append(BitConverter.GetBytes((int)Unit));
    }

    partial void OnUnitAmountChanged(int value) => RecalcOccurrences();
    partial void OnUnitChanged(DateUnit value) => RecalcOccurrences();
}