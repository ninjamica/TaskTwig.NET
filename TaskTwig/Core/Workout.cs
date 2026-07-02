using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Text.Json.Serialization;

namespace TaskTwig.Core;

public record Workout : IHashable
{
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public Dictionary<Exercise, int> Exercises { get; init; } = [];
    
    [JsonIgnore]
    public TimeSpan Length => EndTime.Subtract(StartTime);

    public void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(BitConverter.GetBytes(StartTime.ToBinary()));
        hashAlgorithm.Append(BitConverter.GetBytes(EndTime.ToBinary()));
        
        foreach (var exercisePair in Exercises)
        {
            exercisePair.Key.AppendHash(hashAlgorithm);
            hashAlgorithm.Append(BitConverter.GetBytes(exercisePair.Value));
        }
    }
}