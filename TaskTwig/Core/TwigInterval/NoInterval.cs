using System;
using System.IO.Hashing;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

public class NoInterval : HashableObject, ITwigInterval
{
    /// <inheritdoc/>
    [JsonIgnore]
    public DateOnly? NextOccurrence => null;
    
    /// <inheritdoc/>
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => null;

    /// <inheritdoc/>
    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("NoInterval"u8);
    }
}