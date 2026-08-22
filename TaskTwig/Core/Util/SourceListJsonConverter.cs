using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DynamicData;

namespace TaskTwig.Core.Util;

public class SourceListJsonConverter<T> : JsonConverter<SourceList<T>> where T : notnull
{
    public override SourceList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var items = JsonSerializer.Deserialize<List<T>>(ref reader, options);
        var list = new SourceList<T>();
        
        if (items is not null)
            list.AddRange(items);
        
        return list;
    }

    public override void Write(Utf8JsonWriter writer, SourceList<T> value, JsonSerializerOptions options)
    {
        var list = value.Items.ToList();
        JsonSerializer.Serialize(writer, list, options);
    }
}