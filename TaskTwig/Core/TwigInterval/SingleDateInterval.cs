using System;
using System.IO.Hashing;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core.TwigInterval;

public partial class SingleDateInterval: HashableObject, ITwigInterval
{
    [JsonIgnore]
    public DateOnly? NextOccurrence => Date;
    [JsonIgnore]
    public DateOnly? PreviousOccurrence => null;

    [ObservableProperty]
    public required partial DateOnly Date { get; set; }

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append("SingleDateInterval"u8);
        hashAlgorithm.Append(BitConverter.GetBytes(Date.DayNumber));
    }
}