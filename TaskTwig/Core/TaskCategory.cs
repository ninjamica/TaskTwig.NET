using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class TaskCategory : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "New Task Category";

    [ObservableProperty] public partial Color Color { get; set; } = Color.White;

    // public BindingList<Task> Tasks
    // {
    //     get;
    //     init
    //     {
    //         field = value;
    //         // field.WeakSubscribe((_, _) => _UpdateTodayTasks());
    //         field.ListChanged += fieldOnListChanged;
    //         _RefillTodayTasks();
    //     }
    // } = new();

    public ObservableCollection<Task> Tasks
    {
        get;
        init
        {
            field = value;
            TodayTasks = new FilteredObservableList<Task>(value, task => task.IsToday)
            {
                PropertyNames = ["IsToday"]
            };
        }
    } = [];

    [JsonIgnore] public FilteredObservableList<Task> TodayTasks { get; private set; } = null!;

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