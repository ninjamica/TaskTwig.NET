using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text.Json.Serialization;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.ComponentModel;
using MyToolkit.Collections;
using ObservableCollections;

namespace TaskTwig.Core;

public partial class TaskCategory : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "New Task Category";

    [ObservableProperty] public partial Color Color { get; set; } = System.Drawing.Color.White;

    public BindingList<Task> Tasks
    {
        get;
        init
        {
            field = value;
            // field.WeakSubscribe((_, _) => _UpdateTodayTasks());
            field.ListChanged += fieldOnListChanged;
            _RefillTodayTasks();
        }
    } = new();

    private void fieldOnListChanged(object? sender, ListChangedEventArgs args)
    {
        switch (args.ListChangedType)
        {
            case ListChangedType.ItemDeleted:
                TodayTasks.Remove(Tasks[args.NewIndex]);
                break;
            case ListChangedType.ItemChanged:
                var task = Tasks[args.NewIndex];
                if (TodayTasks.Contains(task))
                {
                    if (!task.IsToday)
                        TodayTasks.Remove(task);
                }
                else
                {
                    _RefillTodayTasks();
                }
                break;
            
            default:
                _RefillTodayTasks();
                break;
        }
    }

    [JsonIgnore] public ObservableCollection<Task> TodayTasks { get; private set; } = [];

    public void AddTask(Task task)
    {
        if (task.Category is not null)
        {
            task.Category.Tasks.Remove(task);
        }
        Tasks.Add(task);
        task.Category = this;
    }

    private void _RefillTodayTasks()
    {
        TodayTasks.Clear();
        foreach (var task in Tasks)
            if (task.IsToday)
                TodayTasks.Add(task);
    }

}