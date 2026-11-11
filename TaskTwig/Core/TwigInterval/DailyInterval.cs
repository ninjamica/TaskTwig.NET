using System;
using System.IO.Hashing;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

public class DailyInterval : HashableObject, ITwigInterval
{
    /// <inheritdoc/>
    [JsonIgnore]
    public DateOnly? NextOccurrence => TwigTime.Today;
    
    /// <inheritdoc/>
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => TwigTime.Today.AddDays(-1);

    /// <inheritdoc/>
    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("DailyInterval"u8);
    }
}