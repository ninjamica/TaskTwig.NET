using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO.Hashing;
using System.Text;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using TaskTwig.Core.Util;

namespace TaskTwig.Core;

public partial class TaskCategory : HashableObject
{
    [ObservableProperty] public partial string Name { get; set; } = "New Task Category";

    [ObservableProperty] public partial Color Color { get; set; } = Color.White;

    [ObservableProperty] public partial bool Expanded { get; set; } = true;
    
    // public int CompletedPoints => DoneTodayTasks.Sum(task  => task.Points);
    // public int TotalPoints => TodayTasks.Sum(task => task.Points) + DoneTodayTasks.Sum(task => task.Points);
    
    
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

    public void AddTask(TwTask task, int? index = null)
    {
        InvalidateCachedHash();
        task.Category?.RemoveTask(task);
        Tasks.Insert(index ?? Tasks.Count, task);
        task.Category = this;
    }

    public void MoveTask(int originalIndex, int destinationIndex)
    {
        InvalidateCachedHash();
        Tasks.Move(originalIndex, destinationIndex);
    }

    public void RemoveTask(TwTask task)
    {
        InvalidateCachedHash();
        Tasks.Remove(task);
    }

    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(Encoding.UTF8.GetBytes(Name));
        hashAlgorithm.Append(BitConverter.GetBytes(Color.ToArgb()));
        hashAlgorithm.Append(BitConverter.GetBytes(Expanded));
    }

    protected override void AppendHashableChildren(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        foreach (var task in Tasks.Items) 
            task.AppendHashAndChildren(mainHasher, childHasher);
    }
}