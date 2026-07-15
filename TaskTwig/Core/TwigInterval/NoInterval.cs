using System;
using System.IO.Hashing;
using System.Text.Json.Serialization;

namespace TaskTwig.Core.TwigInterval;

public class NoInterval : HashableObject, ITwigInterval
{
    [JsonIgnore]
    public DateOnly? NextOccurrence => null;
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => null;

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("NoInterval"u8);
    }
}