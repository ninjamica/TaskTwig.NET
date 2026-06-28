using System.Collections.ObjectModel;
using System.Drawing;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class TaskCategory : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "New Task Category";

    [ObservableProperty] public partial Color Color { get; set; } = Color.White;

    [ObservableProperty] public partial bool Expanded { get; set; } = true;

    public ObservableCollection<TwigTask> Tasks
    {
        get;
        init
        {
            field = value;
            
            if (_todayTasks is not null)
                _todayTasks = new FilteredObservableList<TwigTask>(value, task => task.IsToday, "IsToday");

            if (_doneTodayTasks is not null)
                _doneTodayTasks =
                    new FilteredObservableList<TwigTask>(Tasks, task => task.LastDone.Equals(TaskTwig.Today), "LastDone");
        }
    } = [];
    
    [JsonIgnore]
    public FilteredObservableList<TwigTask> TodayTasks {
        get
        {
            _todayTasks ??= new FilteredObservableList<TwigTask>(Tasks, task => task.IsToday, "IsToday");
            return _todayTasks;
        }
    }
    private FilteredObservableList<TwigTask>? _todayTasks;
    
    [JsonIgnore]
    public FilteredObservableList<TwigTask> DoneTodayTasks {
        get
        {
            _doneTodayTasks ??=
                new FilteredObservableList<TwigTask>(Tasks, task => task.LastDone.Equals(TaskTwig.Today), "LastDone");
            return _doneTodayTasks;
        }
    }
    private FilteredObservableList<TwigTask>? _doneTodayTasks;

    public void AddTask(TwigTask task)
    {
        if (task.Category is not null)
        {
            task.Category.Tasks.Remove(task);
        }
        Tasks.Add(task);
        task.Category = this;
    }
}