using System;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core.TwigInterval;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RepeatPattern
{
    OnAfter,
    OnBefore
}

public abstract partial class RepeatingInterval : HashableObject, ITwigInterval
{
    [ObservableProperty] public partial DateOnly ReferenceDate { get; set; } = TaskTwig.Today;
    [ObservableProperty] public partial bool AutoRepeat { get; set; } = false;
    [ObservableProperty] public partial RepeatPattern RepeatTo { get; set; } = RepeatPattern.OnAfter;
    
    [JsonIgnore]
    [ObservableProperty]
    public partial DateOnly? NextOccurrence { get; private set; }

    [JsonIgnore]
    [ObservableProperty]
    public partial DateOnly? PreviousOccurrence { get; private set; }

    protected RepeatingInterval()
    {
        UpdateOccurrences();
    }

    public DateOnly? NextFromToday()
    {
        DateOnly? date = NextFromDate(ReferenceDate);
        DateOnly today = TaskTwig.Today;
        switch (RepeatTo)
        {
            case RepeatPattern.OnAfter:
                while (date is not null && date.Value.CompareTo(today) < 0)
                    date = NextFromDate(date.Value);
                break;

            case RepeatPattern.OnBefore:
                while (date is not null && date.Value.CompareTo(today) <= 0)
                    date = NextFromDate(date.Value);

                if (date is not null)
                    date = PreviousFromDate(date.Value);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
        return date;
    }

    public DateOnly? PreviousFromToday()
    {
        DateOnly? nextDate = NextFromToday();
        return (nextDate is null) ? null : PreviousFromDate(nextDate.Value);
    }

    public DateOnly? NextFromReference() => NextFromDate(ReferenceDate);
    public DateOnly? PreviousFromReference() => PreviousFromDate(ReferenceDate);

    protected abstract DateOnly? NextFromDate(DateOnly refDate);
    protected abstract DateOnly? PreviousFromDate(DateOnly refDate);

    protected void UpdateOccurrences()
    {
        NextOccurrence = AutoRepeat ? NextFromToday() : NextFromReference();
        PreviousOccurrence = AutoRepeat ? PreviousFromToday() : PreviousFromReference();
    }
    partial void OnReferenceDateChanged(DateOnly value) => UpdateOccurrences();
    partial void OnAutoRepeatChanged(bool value) => UpdateOccurrences();
    partial void OnRepeatToChanged(RepeatPattern value) => UpdateOccurrences();


    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        _AppendHash(hashAlgorithm);
        
        hashAlgorithm.Append(BitConverter.GetBytes(ReferenceDate.DayNumber));
        hashAlgorithm.Append(BitConverter.GetBytes(AutoRepeat));
        hashAlgorithm.Append(BitConverter.GetBytes((int)RepeatTo));
    }
    
    protected abstract void _AppendHash(NonCryptographicHashAlgorithm hashAlgorithm);
}
