using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Input;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Dropbox.Api;
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
    partial void OnDailyJournalChanged(string value) => _todaysJournal.Text = value;
    
    [ObservableProperty] public partial string DailyJournalDate { get; private set; }
    
    public ObservableCollection<Journal> GlobalJournals { get; set; }
    [ObservableProperty] public partial Journal SelectedGlobalJournal { get; set; }

    [RelayCommand]
    public void CreateGlobalJournal()
    {
        var newJournal = new Journal { Title = "New Global Journal" };
        _twig.JournalRecords.GlobalJournals.Add(newJournal);
        SelectedGlobalJournal = newJournal;
    }

    [RelayCommand]
    public void DeleteNote(Journal note)
    {
        _twig.JournalRecords.GlobalJournals.Remove(note);
    }

    public ObservableCollection<TaskCategory> TaskCategoriesView { get; set; }
    public ReadOnlyObservableCollection<TwigTask> DoneTodayTasks { get; set; }

    public NotifyCollectionChangedSynchronizedViewList<KeyValuePair<DateOnly, Sleep>> SleepList { get; init; }
    

    private Core.TaskTwig _twig;
    private Journal _todaysJournal;

    [RelayCommand]
    public void CreateTaskCategory()
    {
        var category = new TaskCategory();
        _twig.TaskCategories.Add(category);
        EditTaskCategory(category);
    }

    [RelayCommand]
    public void EditTaskCategory(TaskCategory category)
    {
        var dialogOptions = new OverlayDialogOptions()
        {
            Mode = DialogMode.Info,
            CanLightDismiss = true,
        };
        var dialogViewModel = new TaskCategoryDialogViewModel(category);
        OverlayDialog.ShowCustomAsync<TaskCategoryDialog, TaskCategoryDialogViewModel, bool>(dialogViewModel, options: dialogOptions)
            .ContinueWith(result =>
            {
                if (result.Result)
                    _twig.TaskCategories.Remove(category);
            });
    }

    [RelayCommand]
    public void DeleteTaskCategory(TaskCategory category)
    {
        _twig.TaskCategories.Remove(category);
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
            Mode = DialogMode.Info,
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
        args.ApplyUpdateMutation();
    }

    [RelayCommand]
    public void TaskListDrop(SortableDropEventArgs args)
    {
        args.IsAccepted = true;
        args.TransferMode = SortableTransferMode.Move;
        args.ApplyDropMutation();
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
        var dialogVm = new DualDateTimeDialogViewModel();
        OverlayDialog.ShowStandardAsync<DualDateTimeDialog, DualDateTimeDialogViewModel>(dialogVm, options:dialogOptions)
            .ContinueWith(task => 
            {
                if (task.Result.HasFlag(DialogResult.OK) && 
                    dialogVm is { StartDateTimeValue: { } startDateTime, EndDateTimeValue: { } endDateTime })
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

    [RelayCommand]
    public async Task SaveFiles()
    {
        IsSaveFilesLoading = true;
        await _twig.WriteDataFiles();
        IsSaveFilesLoading = false;
    }
    [ObservableProperty]
    public partial bool IsSaveFilesLoading { get; private set; } = false;

    [RelayCommand]
    public async Task BackupFiles()
    {
        IsBackupFilesLoading = true;
        await _twig.BackupFiles();
        IsBackupFilesLoading = false;
    }
    [ObservableProperty]
    public partial bool IsBackupFilesLoading { get; private set; } = false;

    [RelayCommand]
    public async Task PushDbx()
    {
        IsPushDbxLoading = true;
        await _twig.PushDbx();
        IsPushDbxLoading = false;
    }
    [ObservableProperty]
    public partial bool IsPushDbxLoading { get; private set; } = false;

    [RelayCommand]
    public async Task PullDbx()
    {
        IsPullDbxLoading = true;
        await _twig.PullDbx();
        IsPullDbxLoading = false;
    }
    [ObservableProperty]
    public partial bool IsPullDbxLoading { get; private set; } = false;

    [RelayCommand]
    public async Task DbxSignIn()
    {
        var oAuth = new PKCEOAuthFlow();
        var url = _twig.DbxHandler.GenDbxAuthUrl(oAuth);
        Console.WriteLine(url.OriginalString);
        
        var dialogOptions = new OverlayDialogOptions()
        {
            Title = "Log In to Dropbox",
            Mode = DialogMode.Question,
            Buttons = DialogButton.OKCancel,
            CanLightDismiss = true,
        };
        var dialogVm = new DbxDialogModelView(url);
        var result = await OverlayDialog.ShowStandardAsync<DbxDialog, DbxDialogModelView>(dialogVm, options:dialogOptions);
        
        if (result.HasFlag(DialogResult.OK) && 
            dialogVm is { CodeText: { } code })
        {
            _twig.DbxHandler.AuthFromCode(oAuth, code);
        }
        
        // try
        // {
        //     DbxAccountName = _twig.DbxHandler.GetAccountName();
        // }
        // catch (InvalidOperationException e)
        // {
        //     DbxAccountName = "No Account";
        // }
    }
    
    [ObservableProperty]
    public partial string DbxAccountName { get; set; }

    public MainViewModel()
    {
        _twig = new Core.TaskTwig();
        _twig.ReadDataFiles();

        _todaysJournal = _twig.TodaysJournal();
        DailyJournal = _todaysJournal.Text;
        DailyJournalDate = Core.TaskTwig.Today.ToString("dddd MMMM d");
        GlobalJournals = _twig.JournalRecords.GlobalJournals;
        if (GlobalJournals.Count > 0)
            SelectedGlobalJournal = GlobalJournals.First();
        TaskCategoriesView = _twig.TaskCategories;
        DoneTodayTasks = _twig.DoneTodayTaskLists;
        SleepList = _twig.SleepRecords.ToNotifyCollectionChanged();

        DbxAccountName = "No Account";
        Task.Run(async () =>
        {
            DbxAccountName = await _twig.DbxHandler.GetAccountName();
        });
        // try
        // {
        //     DbxAccountName = _twig.DbxHandler.GetAccountName();
        // }
        // catch (InvalidOperationException e)
        // {
        //     DbxAccountName = "No Account";
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
        // _twig.WriteDataFiles();
    }
}