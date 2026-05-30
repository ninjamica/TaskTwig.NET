using System;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RepeatPattern
{
    OnAfter,
    OnBefore
}

public abstract class RepeatingInterval : ITwigInterval
{
    [JsonIgnore]
    public DateOnly? NextOccurrence => AutoRepeat ? NextFromToday : NextFromReference;

    [JsonIgnore]
    public DateOnly? PreviousOccurrence => AutoRepeat ? PreviousFromToday : PreviousFromReference;

    [JsonIgnore]
    public DateOnly? NextFromToday
    {
        get
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
    }

    [JsonIgnore]
    public DateOnly? PreviousFromToday
    {
        get
        {
            DateOnly? nextDate = NextFromToday;
            return (nextDate is null) ? null : PreviousFromDate(nextDate.Value);
        }
    }

    [JsonIgnore]
    public DateOnly? NextFromReference => NextFromDate(ReferenceDate);
    [JsonIgnore]
    public DateOnly? PreviousFromReference => PreviousFromDate(ReferenceDate);

    public DateOnly ReferenceDate { get; set; } = TaskTwig.Today;
    public bool AutoRepeat { get; set; } = false;
    public RepeatPattern RepeatTo { get; set; } = RepeatPattern.OnAfter;
    
    protected abstract DateOnly? NextFromDate(DateOnly refDate);
    protected abstract DateOnly? PreviousFromDate(DateOnly refDate);
}