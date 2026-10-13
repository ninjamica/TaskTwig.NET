using System;
using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using JsonException = System.Text.Json.JsonException;

namespace TaskTwig.Core;

[JsonConverter(typeof(ExerciseJsonConverter))]
public readonly struct Exercise
{
    [JsonConverter(typeof(JsonStringEnumConverter<ExerciseUnit>))]
    public enum ExerciseUnit
    {
        Count,
        Seconds,
        Minutes,
        Miles
    }
    
    public required string Name { get; init; }
    public required ExerciseUnit Unit { get; init; }

    public class ExerciseJsonConverter : JsonConverter<Exercise>
    {
        public override Exercise Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadExercise(ref reader, typeToConvert, options);
        }

        public override Exercise ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return ReadExercise(ref reader, typeToConvert, options);
        }
        
        public override void Write(Utf8JsonWriter writer, Exercise value, JsonSerializerOptions options)
        {
            WriteExercise(writer, value, options);
        }

        public override void WriteAsPropertyName(Utf8JsonWriter writer, Exercise value, JsonSerializerOptions options)
        {
            WriteExercise(writer, value, options);
        }
        

        private static Exercise ReadExercise(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var parts = reader.GetString()?.Split(':');
            if (parts is null)
                throw new JsonException("Error Deserializing, expected a string but did not find it.");
            
            return new Exercise()
            {
                Name = parts[0],
                Unit = Enum.Parse<ExerciseUnit>(parts[1])
            };
        }

        private static void WriteExercise(Utf8JsonWriter writer, Exercise value, JsonSerializerOptions options)
        {
            writer.WritePropertyName($"{value.Name}:{value.Unit}");
        }
    }

    public void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Name));
        hashAlgorithm.Append(BitConverter.GetBytes((int)Unit));
    }
}