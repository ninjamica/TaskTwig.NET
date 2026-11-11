using System;
using System.IO.Hashing;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core.TwigInterval;

/// <summary>
/// Determines whether a <see cref="RepeatingInterval"/> represents a period that starts on a certain day, or if it
/// represents a period that ends on a certain day. This is used to determine how the
/// <see cref="RepeatingInterval.ReferenceDate">ReferenceDate</see> should be calculated after repeat intervals.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RepeatPattern
{
    StartsOnDay,
    EndsOnDay
}

public abstract partial class RepeatingInterval : HashableObject, ITwigInterval
{
    /// <summary>
    /// The date that the repeating interval starts or ends on (i.e. the first occurence)
    /// </summary>
    [WatchForHash]
    [ObservableProperty]
    public partial DateOnly? ReferenceDate { get; set; } = TwigTime.Today;
    
    /// <summary>
    /// Whether <see cref="NextOccurrence"/> should automatically repeat based on <see cref="TwigTime.Today"/>,
    /// skipping uncompleted intervals which have fully elapsed.
    /// </summary>
    [WatchForHash]
    [ObservableProperty] 
    public partial bool AutoRepeat { get; set; } = false;
    
    /// <summary>
    /// Determines whether the interval represents a period that starts on <see cref="ReferenceDate"/>, or a period
    /// that ends on <see cref="ReferenceDate"/>. Affects how future occurrences are calculated when the interval is
    /// repeated (including when <see cref="AutoRepeat"/> is set).
    /// <seealso cref="RepeatPattern"/>
    /// </summary>
    [WatchForHash]
    [ObservableProperty] 
    public partial RepeatPattern RepeatTo { get; set; } = RepeatPattern.EndsOnDay;
    
    /// <inheritdoc/>
    [JsonIgnore]
    [ObservableProperty]
    public partial DateOnly? NextOccurrence { get; private set; }

    /// <inheritdoc/>
    [JsonIgnore]
    [ObservableProperty]
    public partial DateOnly? PreviousOccurrence { get; private set; }
    

    /// <summary>
    /// Increments the repeating interval by one interval, such that <see cref="NextOccurrence"/> becomes one interval
    /// after its previous value.
    /// </summary>
    public void IncrementOnce()
    {
        if (ReferenceDate is not null) 
            ReferenceDate = NextAfterDate(ReferenceDate.Value);
    }

    /// <summary>
    /// Decrements the repeating interval by one interval, such that <see cref="NextOccurrence"/> become the previous
    /// value of <see cref="PreviousOccurrence"/>.
    /// </summary>
    public void DecrementOnce()
    {
        if (ReferenceDate is not null)
            ReferenceDate = PreviousBeforeDate(ReferenceDate.Value);
    }
    
    /// <summary>
    /// <para>Increments the repeating interval by skipping any past iterations of the interval.</para>
    /// <para><see cref="NextOccurrence"/> becomes either the next interval start on or after today (if
    /// <see cref="RepeatTo"/> is <see cref="RepeatPattern.StartsOnDay">StartsOnDay</see>) or the next interval end
    /// after today (if <see cref="RepeatTo"/> is <see cref="RepeatPattern.EndsOnDay">EndsOnDay</see>).</para>
    /// </summary>
    /// 
    /// <remarks>
    /// Intended to be used with a <see cref="TwTask"/> with <see cref="AutoExtendPattern.OnCompletion"/>.
    /// </remarks>
    public void SkipThroughToday()
    {
        ReferenceDate = _NextFromToday();
        
        if (RepeatTo == RepeatPattern.EndsOnDay && ReferenceDate == TwigTime.Today)
            IncrementOnce();
    }

    /// <summary>
    /// Resets the interval such that it starts today. If <see cref="RepeatTo"/> is
    /// <see cref="RepeatPattern.EndsOnDay">EndsOnDay</see>, <see cref="NextOccurrence"/> will be set to the interval's
    /// length after today (i.e. the interval starts today but ends after an interval).
    /// </summary>
    /// 
    /// <remarks>
    /// Intended to be used with a <see cref="TwTask"/> with <see cref="AutoExtendPattern.FromCompletion"/>.
    /// </remarks>
    public void RestartFromToday()
    {
        switch (RepeatTo)
        {
            case RepeatPattern.StartsOnDay :
                ReferenceDate = TwigTime.Today;
                break;
            
            case RepeatPattern.EndsOnDay:
                ReferenceDate = NextAfterDate(TwigTime.Today);
                break;
        }
    }
    
    /// <summary>
    /// Recalculates occurrence related properties: <see cref="NextOccurrence"/> and <see cref="PreviousOccurrence"/>.
    /// Derived classes should call this method any time these values may need to be updated. However, this is not
    /// necessary if the reason is a change to a built-in value (e.g. <see cref="ReferenceDate"/>), as those will
    /// update automatically.
    /// </summary>
    public void RecalcOccurrences()
    {
        NextOccurrence = AutoRepeat ? _NextFromToday() : ReferenceDate;
        PreviousOccurrence = NextOccurrence is { } next ? PreviousBeforeDate(next) : null;
    }

    
    /// <summary>
    /// Returns the next occurrence in the interval that is after the provided date.
    /// </summary>
    /// <param name="refDate">The reference date to find the next occurrence after</param>
    /// <returns>The next occurrence after refDate</returns>
    protected abstract DateOnly? NextAfterDate(DateOnly refDate);
    
    /// <summary>
    /// Returns the previous occurrence in the interval that is before the provided date.
    /// </summary>
    /// <param name="refDate">The reference date to find the previous occurrence before</param>
    /// <returns>The previous occurrence before refDate</returns>
    protected abstract DateOnly? PreviousBeforeDate(DateOnly refDate);
    
    /// <summary>
    /// Should be overridden by deriving classes to hash the properties unique to that derived class.
    /// See <see cref="HashableObject"/>.
    /// </summary>
    /// <param name="hashAlgorithm">The hashing algorithm to hash properties with.</param>
    protected abstract void AppendPartialHash(NonCryptographicHashAlgorithm hashAlgorithm);

    
    protected RepeatingInterval()
    {
        RecalcOccurrences();
    }
    
    /// <inheritdoc/>
    protected sealed override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        AppendPartialHash(hashAlgorithm);
        
        hashAlgorithm.Append(BitConverter.GetBytes(ReferenceDate?.DayNumber ?? 0));
        hashAlgorithm.Append(BitConverter.GetBytes(AutoRepeat));
        hashAlgorithm.Append(BitConverter.GetBytes((int)RepeatTo));
    }
    
    // Calculates the next date in the interval, on/before today for StartsOnDay, or after today for EndsOnDay
    private DateOnly? _NextFromToday()
    {
        DateOnly? date = ReferenceDate;
        DateOnly today = TwigTime.Today;
        switch (RepeatTo)
        {
            case RepeatPattern.StartsOnDay:
                DateOnly? nextDate = date;
                while (nextDate <= today)
                {
                    date = nextDate;
                    nextDate = NextAfterDate(nextDate.Value);
                }
                break;
            
            case RepeatPattern.EndsOnDay:
                while (date < today)
                    date = NextAfterDate(date.Value);
                break;
        }
        return date;
    }
    
    
    partial void OnReferenceDateChanged(DateOnly? value) => RecalcOccurrences();
    partial void OnAutoRepeatChanged(bool value) => RecalcOccurrences();
    partial void OnRepeatToChanged(RepeatPattern value) => RecalcOccurrences();
}
