using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservableCollections;
using Sortable.Avalonia;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;
using TaskTwig.Views;
using Ursa.Controls;

namespace TaskTwig.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string DailyJournal { get; set; }
    partial void OnDailyJournalChanged(string value) => _todaysJournal.JournalText = value;
    
    [ObservableProperty] public partial string DailyJournalDate { get; private set; }
    
    public ObservableDictionary<string, Journal> GlobalJournals { get; set; }

    public ObservableCollection<TaskCategory> TaskCategoriesView { get; set; }
    public ReadOnlyObservableCollection<TwigTask> DoneTodayTasks { get; set; }

    public NotifyCollectionChangedSynchronizedViewList<KeyValuePair<DateOnly, Sleep>> SleepList { get; init; }
    

    private Core.TaskTwig _twig;
    private Journal _todaysJournal;

    [RelayCommand]
    public void CreateTaskCategory()
    {
        _twig.TaskCategories.Add(new TaskCategory());
    }

    [RelayCommand]
    public void CreateTask(TaskCategory category)
    {
        var task = new TwigTask()
        {
            Name = "New Task",
            Interval = new NoInterval(),
            Category = category
        };
        category.Tasks.Add(task);
        EditTask(task);
    }
    
    [RelayCommand]
    public void EditTask(TwigTask task)
    {
        var dialogOptions = new OverlayDialogOptions()
        {
            Title = "Edit Task",
            Mode = DialogMode.Info,
            Buttons = DialogButton.OKCancel,
            CanLightDismiss = true,
        };
        var dialogViewModel = new TaskDialogViewModel(task);
        OverlayDialog.ShowCustomAsync<TaskDialog, TaskDialogViewModel, bool>(dialogViewModel, options:dialogOptions)
            .ContinueWith(result =>
            {
                if (result.Result)
                    task.Category.Tasks.Remove(task);
            });
    }

    [RelayCommand]
    public void TaskListUpdate(SortableUpdateEventArgs args)
    {
        Console.WriteLine($"TaskListUpdate: {args}");
        args.ApplyUpdateMutation();
    }

    [RelayCommand]
    public void TaskListDrop(SortableDropEventArgs args)
    {
        Console.WriteLine($"TaskListDrop: {args}");
        args.IsAccepted = true;
        args.TransferMode = SortableTransferMode.Move;
        args.ApplyDropMutation();

        // if (args is { SourceCollection: not null, TargetCollection: not null })
        // {
        //     args.IsAccepted = true;
        //     args.TransferMode = SortableTransferMode.Move;
        //     Console.WriteLine(args.ApplyDropMutation());
        //
        //     // var task = args.SourceCollection[args.OldIndex];
        //     // args.SourceCollection.RemoveAt(args.OldIndex);
        //     // args.TargetCollection.Insert(args.NewIndex, task);
        // }
    }

    [ObservableProperty] public partial string SleepButtonText { get; private set; }

    [RelayCommand]
    public void OnSleepButton()
    {
        var dialogOptions = new OverlayDialogOptions()
        {
            Title = _twig.IsSleeping ? "Enter Wake Up Date/Time" : "Enter Bedtime Date/Time",
            Mode = DialogMode.Question,
            Buttons = DialogButton.OKCancel,
            CanLightDismiss = true,
        };
        var dialogViewModel = new DateTimeDialogViewModel();
        OverlayDialog.ShowStandardAsync<DateTimeDialog, DateTimeDialogViewModel>(dialogViewModel, options:dialogOptions)
            .ContinueWith(task => 
            {
                if (task.Result.HasFlag(DialogResult.OK))
                {
                    OnSleepDateTimeSubmit(dialogViewModel.DateTimeValue);
                }
            });
    }

    [RelayCommand]
    public void OnSleepAddButton()
    {
        var dialogOptions = new OverlayDialogOptions()
        {
            Title = "Enter Bedtime and Wake Up Date/Time",
            Mode = DialogMode.Question,
            Buttons = DialogButton.OKCancel,
            CanLightDismiss = true,
        };
        var dialogVM = new DualDateTimeDialogViewModel();
        OverlayDialog.ShowStandardAsync<DualDateTimeDialog, DualDateTimeDialogViewModel>(dialogVM, options:dialogOptions)
            .ContinueWith(task => 
            {
                if (task.Result.HasFlag(DialogResult.OK) && 
                    dialogVM is { StartDateTimeValue: { } startDateTime, EndDateTimeValue: { } endDateTime })
                {
                    _twig.StartSleeping(startDateTime);
                    _twig.FinishSleeping(endDateTime, true);
                }
            });
    }

    private void OnSleepDateTimeSubmit(DateTime? dateTimeValue)
    {
        var dateTime = dateTimeValue ?? DateTime.Now;
        if (_twig.IsSleeping)
        {
            if (!_twig.FinishSleeping(dateTime, false))
            {
                _twig.FinishSleeping(dateTime, true);
            }
        }
        else
        {
            _twig.StartSleeping(dateTime);
        }
    }

    public MainViewModel()
    {
        _twig = new Core.TaskTwig();
        _twig.ReadDataFiles();

        _todaysJournal = _twig.TodaysJournal();
        DailyJournal = _todaysJournal.JournalText;
        DailyJournalDate = Core.TaskTwig.Today.ToString("dddd MMMM d");
        GlobalJournals = _twig.JournalRecords.GlobalJournals;
        TaskCategoriesView = _twig.TaskCategories;
        DoneTodayTasks = _twig.DoneTodayTaskLists;
        SleepList = _twig.SleepRecords.ToNotifyCollectionChanged();

        // foreach (var category in _twig.TaskCategories)
        // {
        //     TasksToday[category] = IsTodayFilter(category);
        // }
        
        SleepButtonText = _twig.IsSleeping ? "Wake Up" : "Go To Sleep";

        _twig.PropertyChanged += OnTwigOnPropertyChanged;
    }

    private void OnTwigOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender == _twig)
        {
            if (args.PropertyName == nameof(_twig.IsSleeping))
            {
                SleepButtonText = _twig.IsSleeping ? "Wake Up" : "Go To Sleep";
            }
        }
    }

    public void Cleanup()
    {
        _twig.WriteDataFiles();
    }
}