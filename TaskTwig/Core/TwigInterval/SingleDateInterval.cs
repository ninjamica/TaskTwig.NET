using System;
using System.IO.Hashing;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core.TwigInterval;

public partial class SingleDateInterval: HashableObject, ITwigInterval
{
    /// <summary>
    /// The singular date that this interval occurs at.
    /// </summary>
    [WatchForHash]
    [ObservableProperty]
    public required partial DateOnly Date { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public DateOnly? NextOccurrence => Date;

    /// <inheritdoc/>
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => null;

    /// <inheritdoc/>
    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("SingleDateInterval"u8);
        hashAlgorithm.Append(BitConverter.GetBytes(Date.DayNumber));
    }
}