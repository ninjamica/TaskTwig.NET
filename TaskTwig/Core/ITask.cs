using System;

namespace TaskTwig.Core;

public interface ITask
{
    string Name { get; set; }
    bool IsDone { get; set; }
}