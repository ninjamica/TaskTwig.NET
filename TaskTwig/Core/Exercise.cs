using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Transactions;

namespace TaskTwig.Core;

[JsonConverter(typeof(ExerciseJsonConverter))]
public readonly record struct Exercise()
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
            // return JsonSerializer.Deserialize<Exercise>(ref reader, options);
            string[] parts = reader.GetString().Split(':');
            return new Exercise()
            {
                Name = parts[0],
                Unit = Enum.Parse<ExerciseUnit>(parts[1])
            };
        }
    
        public override void Write(Utf8JsonWriter writer, Exercise value, JsonSerializerOptions options)
        {
            // JsonSerializer.Serialize(writer, value, value.GetType(), options);
            Console.WriteLine($"{value.Name}:{value.Unit}");
            writer.WriteStringValue($"{value.Name}:{value.Unit}");
        }
    
        public override Exercise ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            string[] parts = reader.GetString().Split(':');
            return new Exercise()
            {
                Name = parts[0],
                Unit = Enum.Parse<ExerciseUnit>(parts[1])
            };
        }
        
        public override void WriteAsPropertyName(Utf8JsonWriter writer, Exercise value, JsonSerializerOptions options)
        {
            Console.WriteLine($"{value.Name}:{value.Unit}");
            writer.WritePropertyName($"{value.Name}:{value.Unit}");
        }
    }
}