using System;
using System.IO.Hashing;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

public class DailyInterval : HashableObject, ITwigInterval
{
    // TODO: Deal with TaskTwig.Today being possibly observable
    [JsonIgnore]
    public DateOnly? NextOccurrence => TwigTime.Today;
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => TwigTime.Today.AddDays(-1);

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("DailyInterval"u8);
    }
}