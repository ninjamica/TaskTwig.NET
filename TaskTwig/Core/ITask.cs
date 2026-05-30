using System;
using System.Text.Json.Serialization;

namespace TaskTwig.Core;

public interface ITask
{
    [JsonIgnore]
    string Name { get; set; }
    [JsonIgnore]
    bool IsDone { get; set; }
}