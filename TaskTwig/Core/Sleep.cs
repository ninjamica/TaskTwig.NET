using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Hashing;
using System.Text.Json.Serialization;

namespace TaskTwig.Core;

public readonly record struct Sleep : IHashable
{
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }
    
    [SetsRequiredMembers]
    public Sleep(DateTime startTime, DateTime endTime)
    {
        StartTime = startTime;
        EndTime = endTime;
    }
    
    [JsonIgnore]
    public TimeSpan Length => EndTime.Subtract(StartTime);

    [JsonIgnore]
    public DateOnly Date => DateOnly.FromDateTime(EndTime).AddDays(-1);

    public void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(BitConverter.GetBytes(StartTime.ToBinary()));
        hashAlgorithm.Append(BitConverter.GetBytes(EndTime.ToBinary()));
    }
}
