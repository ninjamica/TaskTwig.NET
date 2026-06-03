using System.Collections.ObjectModel;
using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class TaskCategory : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "New Task Category";

    [ObservableProperty] public partial Color Color { get; set; } = System.Drawing.Color.White;
    
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