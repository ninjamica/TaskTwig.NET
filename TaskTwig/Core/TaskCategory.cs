using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO.Hashing;
using System.Text;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class TaskCategory : HashableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "New Task Category";

    [ObservableProperty] public partial Color Color { get; set; } = Color.White;

    [ObservableProperty] public partial bool Expanded { get; set; } = true;

    public ObservableCollection<TwTask> Tasks
    {
        get;
        init
        {
            field = value;
            
            if (_todayTasks is not null)
                _todayTasks = new FilteredObservableList<TwTask>(value, task => task.IsToday, "IsToday");

            if (_doneTodayTasks is not null)
                _doneTodayTasks =
                    new FilteredObservableList<TwTask>(Tasks, task => task.LastDone.Equals(TaskTwig.Today), "LastDone");
        }
    } = [];
    
    [JsonIgnore]
    public FilteredObservableList<TwTask> TodayTasks {
        get
        {
            _todayTasks ??= new FilteredObservableList<TwTask>(Tasks, task => task.IsToday, "IsToday");
            return _todayTasks;
        }
    }
    private FilteredObservableList<TwTask>? _todayTasks;
    
    [JsonIgnore]
    public FilteredObservableList<TwTask> DoneTodayTasks {
        get
        {
            _doneTodayTasks ??=
                new FilteredObservableList<TwTask>(Tasks, task => task.LastDone.Equals(TaskTwig.Today), "LastDone");
            return _doneTodayTasks;
        }
    }
    private FilteredObservableList<TwTask>? _doneTodayTasks;

    public void AddTask(TwTask task)
    {
        if (task.Category is not null)
        {
            task.Category.Tasks.Remove(task);
        }
        Tasks.Add(task);
        task.Category = this;
    }

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Name));
        hashAlgorithm.Append(BitConverter.GetBytes(Color.ToArgb()));
        hashAlgorithm.Append(BitConverter.GetBytes(Expanded));
        
        // foreach (var task in Tasks) 
        //     task.AppendHash(hashAlgorithm);
    }

    protected override void AppendHashableChildren(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        foreach (var task in Tasks) 
            task.AppendHashAndChildren(mainHasher, childHasher);
    }
}