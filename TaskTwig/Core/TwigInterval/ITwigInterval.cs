using System;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

[JsonDerivedType(typeof(NoInterval), typeDiscriminator: "NoDate")]
[JsonDerivedType(typeof(SingleDateInterval), typeDiscriminator: "SingleDate")]
[JsonDerivedType(typeof(DailyInterval), typeDiscriminator: "Daily")]
[JsonDerivedType(typeof(UnitInterval), typeDiscriminator: "Unit")]
[JsonDerivedType(typeof(WeekInterval), typeDiscriminator: "Week")]
[JsonDerivedType(typeof(MonthInterval), typeDiscriminator: "Month")]
public interface ITwigInterval
{
    [JsonIgnore]
    DateOnly? NextOccurrence { get; }
    [JsonIgnore]
    DateOnly? PreviousOccurrence { get; }
}