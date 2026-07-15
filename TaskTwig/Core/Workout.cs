using System;
using System.Collections.Generic;
using System.IO.Hashing;
using System.Linq;
using System.Text.Json.Serialization;
using ObservableCollections;

namespace TaskTwig.Core;

public class Workout : HashableObject
{
    public required DateTime StartTime { get; init; }
    public required DateTime EndTime { get; init; }

    public required ObservableDictionary<Exercise, int> Exercises
    {
        get;
        init
        {
            field = value;
            field.CollectionChanged += (in _) => InvalidateCachedHash();
        }
    } = [];

    [JsonIgnore]
    public TimeSpan Length => EndTime.Subtract(StartTime);

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(BitConverter.GetBytes(StartTime.ToBinary()));
        hashAlgorithm.Append(BitConverter.GetBytes(EndTime.ToBinary()));
        
        foreach (var exercisePair in Exercises.OrderBy(pair => pair.Key))
        {
            exercisePair.Key.AppendHash(hashAlgorithm);
            hashAlgorithm.Append(BitConverter.GetBytes(exercisePair.Value));
        }
    }

    // protected override void AppendHashableChildren(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    // {
    //     foreach (var exercisePair in Exercises.OrderBy(pair => pair.Key))
    //     {
    //         exercisePair.Key.AppendHashAndChildren(mainHasher, childHasher);
    //         mainHasher.Append(BitConverter.GetBytes(exercisePair.Value));
    //     }
    // }
}