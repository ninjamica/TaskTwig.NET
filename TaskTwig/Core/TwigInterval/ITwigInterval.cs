using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

[JsonDerivedType(typeof(NoInterval), typeDiscriminator: "NoDate")]
[JsonDerivedType(typeof(SingleDateInterval), typeDiscriminator: "SingleDate")]
[JsonDerivedType(typeof(DailyInterval), typeDiscriminator: "Daily")]
[JsonDerivedType(typeof(UnitInterval), typeDiscriminator: "Unit")]
[JsonDerivedType(typeof(WeekInterval), typeDiscriminator: "Week")]
[JsonDerivedType(typeof(MonthInterval), typeDiscriminator: "Month")]
public interface ITwigInterval : INotifyPropertyChanged
{
    /// <summary>
    /// The next date that the interval occurs on. A <c>null</c> value means there is no future occurence.
    /// </summary>
    [JsonIgnore]
    DateOnly? NextOccurrence { get; }
    
    /// <summary>
    /// The date of the previous interval occurence before <see cref="NextOccurrence"/>.
    /// A <c>null</c> value means there is no past occurence.
    /// </summary>
    [JsonIgnore]
    DateOnly? PreviousOccurrence { get; }
}