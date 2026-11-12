using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO.Hashing;
using System.Text;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using TaskTwig.Core.TwigInterval;
using TaskTwig.Core.Util;

namespace TaskTwig.Core;

public partial class TaskCategory : HashableObject
{
    [WatchForHash]
    [ObservableProperty] 
    public partial string Name { get; set; } = "New Task Category";
    
    [WatchForHash]
    [ObservableProperty]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public partial string? Icon { get; set; }
    
    [ObservableProperty]
    public partial bool Expanded { get; set; } = true;
    
    // public int CompletedPoints => DoneTodayTasks.Sum(task  => task.Points);
    // public int TotalPoints => TodayTasks.Sum(task => task.Points) + DoneTodayTasks.Sum(task => task.Points);
    
    
    [WatchForHash]
    [JsonInclude]
    [JsonConverter(typeof(SourceListJsonConverter<TwTask>))]
    private SourceList<TwTask> Tasks { get; init; } = new();
    
    public IObservable<IChangeSet<TwTask>> ConnectTasks() => Tasks.Connect();
    
    [JsonIgnore]
    public ReadOnlyObservableCollection<TwTask> TasksView 
    {
        get
        {
            if (field is null)
            {
                Tasks.Connect().Bind(out field).Subscribe();
            }
            return field;
        }
    }
    
    [JsonIgnore]
    public IObservableList<TwTask> TasksObservableList {
        get
        {
            field ??= Tasks.AsObservableList();
            return field;
        }
    }
    
    [JsonIgnore]
    public int Count => Tasks.Count;
    
    [JsonIgnore]
    public ReadOnlyObservableCollection<TwTask> TodayTasks 
    {
        get
        {
            if (field is null)
            {
                Tasks.Connect()
                    .AutoRefresh()
                    .Filter(task => task.IsToday)
                    .Bind(out field)
                    .Subscribe();
            }

            return field;
        }
    }

    public void AddTask(TwTask task)
    {
        AddTask(task, Tasks.Count);
    }

    public void AddTask(TwTask task, int index)
    {
        InvalidateCachedHash();
        
        if (task.Category != this)
            task.Category?.RemoveTask(task);
        
        Tasks.Insert(index, task);
        task.Category = this;

        Console.WriteLine($"{Name}: AddTask {task.Name}:{index}");
    }

    public void MoveTask(int originalIndex, int destinationIndex)
    {
        if (originalIndex < 0 || originalIndex >= Tasks.Count || 
            destinationIndex < 0 || destinationIndex >= Tasks.Count)
            return;
        
        Tasks.Move(originalIndex, destinationIndex);
        InvalidateCachedHash();

        Console.WriteLine($"{Name}: MoveTask {originalIndex}:{destinationIndex}");
    }

    public void MoveTask(TwTask task, int destinationIndex) => MoveTask(Tasks.Items.IndexOf(task), destinationIndex);

    public void RemoveTask(TwTask task)
    {
        if(Tasks.Remove(task))
        {
            task.Category = null;
            InvalidateCachedHash();

            Console.WriteLine($"{Name}: RemoveTask {task.Name}");
        }
    }

    public void RemovePermaDoneTasks()
    {
        Tasks.Edit(list =>
        {
            foreach (var task in list)
            {
                if (task is { IsDone: true, Interval: NoInterval or SingleDateInterval })
                {
                    list.Remove(task);
                }
            }
        });
        InvalidateCachedHash();
    }

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Name));
        if (!string.IsNullOrEmpty(Icon))
            hashAlgorithm.Append(Encoding.UTF8.GetBytes(Icon));
        hashAlgorithm.Append(BitConverter.GetBytes(Expanded));
    }

    protected override void AppendHashableChildren(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        foreach (var task in Tasks.Items) 
            task.AppendHashAndChildren(mainHasher, childHasher);
    }
}