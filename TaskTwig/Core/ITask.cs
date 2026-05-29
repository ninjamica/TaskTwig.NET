using System;

namespace TaskTwig.Core;

public interface ITask
{
    string Name { get; set; }
    DateOnly? LastDone { get; set; }
}