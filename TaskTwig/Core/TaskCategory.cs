using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class TaskCategory : ObservableObject
{
    [ObservableProperty] 
    public required partial string Name { get; set; }
    
    [ObservableProperty]
    public required partial Color Color { get; set; }
    
    public ObservableCollection<Task> Tasks { get; init; } = [];

    public void AddTask(Task task)
    {
        if (task.Category is not null)
        {
            task.Category.Tasks.Remove(task);
        }
        Tasks.Add(task);
        task.Category = this;
    }
}