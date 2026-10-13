using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using DynamicData.Kernel;
using Sortable.Avalonia;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;
using TaskTwig.Dialog.ViewModels;
using TaskTwig.Dialog.Views;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;
using DateTimeDialog = TaskTwig.Dialog.Views.DateTimeDialog;
using DbxDialog = TaskTwig.Dialog.Views.DbxDialog;
using DualDateTimeDialog = TaskTwig.Dialog.Views.DualDateTimeDialog;
using SyncConflictDialog = TaskTwig.Dialog.Views.SyncConflictDialog;
using TaskCategoryDialog = TaskTwig.Dialog.Views.TaskCategoryDialog;
using TaskDialog = TaskTwig.Dialog.Views.TaskDialog;

namespace TaskTwig.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly Core.TaskTwig _twig;


    private readonly TaskCategoryDialog _taskCategoryEditDialog = new();
    private readonly TaskCategoryDialogViewModel _taskCategoryEditDialogVm = new();

    private readonly TaskDialog _taskEditDialog = new();
    private readonly TaskDialogViewModel _taskEditDialogVm = new();

    [ObservableProperty]
    public partial bool TodayDoneExpanded { get; set; } = true;

    private readonly ReadOnlyObservableCollection<TaskCategory> _taskCategoriesView;
    public ReadOnlyObservableCollection<TaskCategory> TaskCategoriesView => _taskCategoriesView;
    
    private readonly ReadOnlyObservableCollection<TwTask> _doneTodaytasks;
    public ReadOnlyObservableCollection<TwTask> DoneTodayTasks => _doneTodaytasks;

    private readonly ReadOnlyObservableCollection<Sleep> _sleepList;
    public ReadOnlyObservableCollection<Sleep> SleepList => _sleepList;
    
    private readonly ReadOnlyObservableCollection<Note> _notes;
    public ReadOnlyObservableCollection<Note> Notes => _notes;
    
    [RelayCommand]
    private void CreateTaskCategory()
    {
        var category = new TaskCategory();
        _twig.TaskCategories.Add(category);
        EditTaskCategory(category);
    }

    [RelayCommand]
    private void EditTaskCategory(TaskCategory category)
    {
        var drawerOptions = new DrawerOptions()
        {
            Position = Position.Top,
            CanLightDismiss = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        
        _taskCategoryEditDialogVm.Category = category;
        OverlayDrawer.ShowCustomAsync<bool>(_taskCategoryEditDialog, _taskCategoryEditDialogVm, options: drawerOptions)
            .ContinueWith(result =>
            {
                if (result.Result)
                    _twig.TaskCategories.Remove(category);
            });
    }

    [RelayCommand]
    private void DeleteTaskCategory(TaskCategory category)
    {
        _twig.TaskCategories.Remove(category);
    }

    [RelayCommand]
    private void CreateTask(TaskCategory category)
    {
        var task = new TwTask()
        {
            Name = "New Task",
            Interval = new NoInterval(),
            Category = category
        };
        category.AddTask(task);
        category.Expanded = true;
        EditTask(task);
    }
    
    [RelayCommand]
    private void EditTask(TwTask task)
    {
        var drawerOptions = new DrawerOptions()
        {
            Position = Position.Top,
            CanLightDismiss = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        
        _taskEditDialogVm.Task = task;
        OverlayDrawer.ShowCustomAsync<bool>(_taskEditDialog, _taskEditDialogVm, options:drawerOptions)
            .ContinueWith(result =>
            {
                if (result.Result)
                    task.Category?.RemoveTask(task);
            });
    }
    
    [RelayCommand]
    private void CategoryListUpdate(SortableUpdateEventArgs args)
    {
        if (args.Item is TaskCategory category)
        {
            _twig.TaskCategories.Edit(list =>
            {
                list.Remove(category);
                list.Insert(args.NewIndex, category);
            });
        }
    }
    
    [RelayCommand]
    private void TaskListUpdate(SortableUpdateEventArgs args)
    {
        if (args.Item is TwTask { Category: { } category })
        {
            category.MoveTask(args.OldIndex, args.NewIndex);
        }
    }

    [RelayCommand]
    private void TaskListDrop(SortableDropEventArgs args)
    {
        var targetCategory = _twig.TaskCategories.Items.FirstOrDefault(
            cat => ReferenceEquals(cat.TasksView, args.TargetCollection));

        if (targetCategory is not null && args.Item is TwTask task)
        {
            args.IsAccepted = true;
            args.TransferMode = SortableTransferMode.Move;
                
            targetCategory.AddTask(task, args.NewIndex);
        }
        else
        {
            args.IsAccepted = false;
        }
    }

    [RelayCommand]
    private void TaskListRelease(SortableReleaseEventArgs args)
    {
        if (args.LastPreviewedIndex != args.OldIndex && args.Item is TwTask { Category: { } category })
        {
            if (args.OldIndex != -1)
            {
                category.MoveTask(args.OldIndex, args.LastPreviewedIndex);
            }
            // TODO: currently no way to determine last previewed collection (where to drop the item)
            // else { }
        }
    }

    [RelayCommand]
    private void DeletePermaDoneTasks()
    {
        foreach (var category in TaskCategoriesView)
        {
            category.RemovePermaDoneTasks();
        }
    }

    [ObservableProperty] 
    public partial bool IsSleeping { get; private set; }

    [RelayCommand]
    private void OnSleepButton()
    {
        var dialogOptions = new OverlayDialogOptions()
        {
            Title = IsSleeping ? "Enter Wake Up Date/Time" : "Enter Bedtime Date/Time",
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
    private void CancelSleep()
    {
        _twig.SleepValues.CancelSleep();
    }

    [RelayCommand]
    private void OnSleepAddButton()
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
                    _twig.SleepValues.StartSleeping(startDateTime);
                    _twig.SleepValues.FinishSleeping(endDateTime, true);
                }
            });
    }

    private void OnSleepDateTimeSubmit(DateTime? dateTimeValue)
    {
        var dateTime = dateTimeValue ?? DateTime.Now;
        if (_twig.SleepValues.IsSleeping)
        {
            if (!_twig.SleepValues.FinishSleeping(dateTime, false))
            {
                _twig.SleepValues.FinishSleeping(dateTime, true);
            }
        }
        else
        {
            _twig.SleepValues.StartSleeping(dateTime);
        }
    }
    
    [ObservableProperty]
    public partial Journal? SelectedJournal { get; private set; }
    
    [ObservableProperty]
    public partial DateTime? JournalSelectedDate { get; set; }

    partial void OnJournalSelectedDateChanged(DateTime? value)
    {
        if (value is { } date)
        {
            SelectedJournal = _twig.Journals.Lookup(DateOnly.FromDateTime(date)).ValueOrDefault();
        }
    }

    public CalendarBlackoutDatesCollection? JournalBlackoutDates
    {
        get;
        set
        {
            field = value;
            UpdateJournalBlackoutDates();
        }
    }

    private void UpdateJournalBlackoutDates()
    {
        if (JournalBlackoutDates is null) 
            return;
        
        var dates = _twig.Journals.Keys.Order().ToList();
        
        JournalBlackoutDates.Clear();
        
        if (dates.Count == 0)
            return;
        
        JournalBlackoutDates.Add(new CalendarDateRange(DateTime.MinValue, dates.First().AddDays(-1).ToDateTime(TimeOnly.MinValue)));

        for (int i = 0; i < dates.Count - 1; i++)
        {
            var current = dates[i];
            var next = dates[i + 1];
            
            if (next.DayNumber - current.DayNumber > 1)
                JournalBlackoutDates.Add(new CalendarDateRange(current.AddDays(1).ToDateTime(TimeOnly.MinValue), 
                                                               next.AddDays(-1).ToDateTime(TimeOnly.MinValue)));
        }
        
        JournalBlackoutDates.Add(new CalendarDateRange(dates.Last().AddDays(1).ToDateTime(TimeOnly.MinValue), DateTime.MaxValue));
    }
    
    [ObservableProperty] public partial Note? SelectedNote { get; set; }

    [RelayCommand]
    private void CreateNote()
    {
        var newNote = new Note { Title = "New Note" };
        _twig.Notes.Add(newNote);
        SelectedNote = newNote;
    }

    [RelayCommand]
    private void UpdateNoteList(SortableUpdateEventArgs args)
    {
        if (args.Item is Note)
        {
            _twig.Notes.Move(args.OldIndex, args.NewIndex);
        }
    }

    [RelayCommand]
    private void DeleteNote(Note note)
    {
        _twig.Notes.Remove(note);
    }

    [RelayCommand]
    private void ShowNotesDrawer()
    {
        var drawerOptions = new DrawerOptions()
        {
            Title = "Notes",
            Position = Position.Left,
            Buttons = DialogButton.None,
            CanLightDismiss = true
        };
        var drawerVm = new NotesDrawerViewModel(this);
        OverlayDrawer.ShowStandardAsync<NotesDrawer, NotesDrawerViewModel>(drawerVm, options: drawerOptions);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PushDbxCommand), nameof(PullDbxCommand), 
                                nameof(DbxSyncCommand), nameof(FetchDbxCommand))]
    public partial bool IsDbxConnected { get; set; } = false;

    [ObservableProperty]
    public partial string? DbxAccountName { get; private set; }
    
    [ObservableProperty]
    public partial string? SyncStatusText { get; private set; }
    
    [ObservableProperty]
    public partial bool CacheValid { get; private set; }
    
    [ObservableProperty]
    public partial bool InSync { get; private set; }

    public Task<IImage?>? DbxPhoto
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    private async Task<IImage?> _getDbxPhoto(string? url)
    {
        if (!IsDbxConnected || url is null)
            return null;
        
        var client = _twig.DbxHandler.DbxClientConfig.HttpClient;
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var data = await response.Content.ReadAsByteArrayAsync();
        
        return new Bitmap(new MemoryStream(data));
    }

    [ObservableProperty]
    public partial Task<ReadOnlyObservableCollection<TaskCategory>>? GetTaskCategoriesAsync { get; private set; }

    public void LoadTaskCategoriesAsync()
    {
        if (GetTaskCategoriesAsync is null)
        {
            GetTaskCategoriesAsync = _getTaskCategoriesAsync();
            OnPropertyChanged(nameof(GetTaskCategoriesAsync));
        }
    }

    private async Task<ReadOnlyObservableCollection<TaskCategory>> _getTaskCategoriesAsync()
    {
        await Dispatcher.UIThread.Resume(DispatcherPriority.Background);
        return TaskCategoriesView;
    }
    

    [RelayCommand]
    private async Task DbxSignInOut()
    {
        if (IsDbxConnected)
        {
            var confirm = await OverlayMessageBox.ShowAsync(
                "Are you sure you want to sign out?",
                title: "Sign Out?",
                icon: MessageBoxIcon.Question,
                button: MessageBoxButton.YesNo
            );
            
            if (confirm == MessageBoxResult.Yes)
            {
                await _twig.DbxHandler.Logout();
            }
        }
        else
        {
            var (uri, oAuth) = _twig.DbxHandler.GenDbxAuthUrl();
            Console.WriteLine(uri.OriginalString);

            var dialogOptions = new OverlayDialogOptions()
            {
                Title = "Log In to Dropbox",
                Mode = DialogMode.Question,
                Buttons = DialogButton.OKCancel,
                CanLightDismiss = true,
            };
            var dialogVm = new DbxDialogModelView(uri);
            var result =
                await OverlayDialog.ShowStandardAsync<DbxDialog, DbxDialogModelView>(dialogVm, options: dialogOptions);

            if (result.HasFlag(DialogResult.OK) &&
                dialogVm is { CodeText: { } code })
            {
                await _twig.DbxHandler.AuthFromCode(oAuth, code);
            }
        }
        
    }

    [RelayCommand]
    private async Task SaveFiles()
    {
        try
        {
            HashableObject.StopSaveTimer();
            var files = await _twig.SaveDataFiles();
            // NotificationManager?.Show(new Notification("Saving Completed", string.Join(',', files)),
            //     NotificationType.Success);

            SyncStatusText = "Saved";
            if (files.Count > 0)
                SyncStatusText += $": {string.Join(',', files)}";
        }
        catch (TwigInvalidOperationException)
        {
            // NotificationManager?.Show(new Notification("Save Canceled", "Data operation already in progress"),
            //     NotificationType.Warning);
            
            SyncStatusText = "Save Canceled (data operation already in progress)";
        }
    }

    [RelayCommand(CanExecute = nameof(IsDbxConnected))]
    private async Task PushDbx()
    {
        try
        {
            SyncStatusText = "Pushing to Dropbox";
            
            HashableObject.StopSaveTimer();
            await _twig.PushDbx();
            InSync = CacheValid;
            
            SyncStatusText = "Pushed to Dropbox";
        }
        catch (TwigInvalidOperationException)
        {
            SyncStatusText = "Pushed Canceled (data operation already in progress)";
        }
    }

    [RelayCommand(CanExecute = nameof(IsDbxConnected))]
    private async Task PullDbx()
    {
        try
        {
            SyncStatusText = "Pulling from Dropbox";
            
            HashableObject.StopSaveTimer();
            await _twig.PullDbx();
            InSync = CacheValid;
            
            SyncStatusText = "Pulled from Dropbox";
        }
        catch (TwigInvalidOperationException)
        {
            SyncStatusText = "Pull Canceled (data operation already in progress)";
        }
    }

    [RelayCommand(CanExecute = nameof(IsDbxConnected))]
    private async Task FetchDbx()
    {
        try
        {
            SyncStatusText = "Fetching from Dropbox";
            
            HashableObject.StopSaveTimer();
            var diffs = await _twig.FetchDbx();

            if (diffs.Count > 0)
            {
                InSync = false;
                SyncStatusText = $"Out of Sync: {string.Join(", ", 
                    diffs.Select(pair => $"{pair.Key}{(pair.Value == DataFileAction.Download ? "↓" : "↑")}"))}";
            }
            else
            {
                InSync = CacheValid;
                SyncStatusText = "In Sync With Dropbox";
            }
        }
        catch (TwigInvalidOperationException)
        {
            SyncStatusText = "Fetch Canceled (data operation already in progress)";
        }
    }

    private static async Task<Dictionary<DataFile, DataFileAction>?> SyncConflictCallback(
        Dictionary<DataFile, DataFileAction> actions)
    {
        var dialogOptions = new OverlayDialogOptions()
        {
            Title = "Sync Conflict!", Mode = DialogMode.Question, Buttons = DialogButton.OKCancel, CanLightDismiss = false,
        };
        var dialogVm = new SyncConflictDialogViewModel(actions);
        var result = await OverlayDialog.ShowStandardAsync<SyncConflictDialog, SyncConflictDialogViewModel>(dialogVm, options: dialogOptions);

        return result.HasFlag(DialogResult.OK) ? dialogVm.GetActions() : null;
    }

    [RelayCommand(CanExecute = nameof(IsDbxConnected))]
    private async Task DbxSync()
    {
        try
        {
            var progress = new Progress<SyncProgress>(syncProgress =>
            {
                switch (syncProgress.Stage)
                {
                    case SyncProgressStage.Hash:
                        SyncStatusText = "Hashing Files";
                        break;

                    case SyncProgressStage.Save:
                        SyncStatusText = "Saving Files: ";

                        SyncStatusText += syncProgress.SyncFiles is { } files && files.Any()
                            ? string.Join(", ", files)
                            : "Nothing to save";
                        break;

                    case SyncProgressStage.Compare:
                        SyncStatusText = "Comparing Files to Cloud";
                        break;

                    case SyncProgressStage.Sync:
                        SyncStatusText = "Syncing Files: ";

                        SyncStatusText += syncProgress.SyncActions is { Count: > 0 } actions
                            ? string.Join(", ",
                                actions.Select(pair =>
                                    $"{pair.Key}{(pair.Value == DataFileAction.Download ? "↓" : "↑")}"))
                            : "Nothing to do";
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            });

            HashableObject.StopSaveTimer();
            var actions = await _twig.SyncWithDbx(SyncConflictCallback, progress);

            if (actions is null)
            {
                InSync = false;
                SyncStatusText = "Sync Canceled";
            }
            else
            {
                InSync = CacheValid;
                SyncStatusText = SyncStatusText?.Replace("Syncing Files: ", "Sync Completed: ");
            }
        }
        catch (TwigInvalidOperationException)
        {
            SyncStatusText = "Sync Canceled (data operation already in progress)";
        }
    }
    
    [ObservableProperty]
    public partial TimeSpan DayStart { get; set; } = TwigTime.DayStart;
    partial void OnDayStartChanged(TimeSpan value) => TwigTime.DayStart = value;

    [ObservableProperty]
    public partial bool AutoSync { get; set; }
    partial void OnAutoSyncChanged(bool value) => _twig.AutoSync = value;

    public MainViewModel()
    {
        _twig = new Core.TaskTwig();
        _twig.PropertyChanged += OnTwigPropertyChanged;
        _twig.SleepValues.PropertyChanged += OnSleepPropertyChanged;
        TwigTime.OnTodayChanged += OnTodayChanged;
        _twig.DbxHandler.AccountChanged += DbxHandlerOnAccountChanged;
        HashableObject.CacheValidChanged += CacheValidChanged;
        
        _twig.InitDataFromFiles().ContinueWith(_ =>
        {
            if (Notes.Count > 0 && (SelectedNote is null || !Notes.Contains(SelectedNote)))
                SelectedNote = Notes.First();
            
            _twig.Journals.Connect().Subscribe(JournalsOnCollectionChanged);
            JournalSelectedDate = _twig.TodaysJournal().Date.ToDateTime(TimeOnly.MinValue);
            Dispatcher.UIThread.Post(UpdateJournalBlackoutDates);
            HashableObject.SaveCallback = OnSaveTimerTick;

            if (IsDbxConnected)
                Dispatcher.UIThread.InvokeAsync(() => FetchDbxCommand.ExecuteAsync(null));
        });
        
        _twig.TaskCategories.Connect()
            .Bind(out _taskCategoriesView).Subscribe();
        _twig.TaskCategories.Connect()
            .MergeManyChangeSets(category => category.ConnectTasks())
            .AutoRefresh()
            .AutoRefreshOnObservable(_ => TwigTime.TodayChangedReevaluator)
            .Filter(task => task.LastDone.Equals(TwigTime.Today))
            .Bind(out _doneTodaytasks)
            .DisposeMany()
            .Subscribe();
        _twig.SleepValues.SleepRecords.Connect()
            .AutoRefresh()
            .SortAndBind(out _sleepList, SortExpressionComparer<Sleep>.Descending(sleep => sleep.Date))
            .DisposeMany()
            .Subscribe();
        IsSleeping = _twig.SleepValues.IsSleeping;
        _twig.Notes.Connect().Bind(out _notes).Subscribe();

        Task.Run(async () => await _twig.DbxHandler.AuthFromStoredKeys());
    }

    private void OnSaveTimerTick()
    {
        Dispatcher.UIThread.InvokeAsync(async () =>
        {
            if (AutoSync)
                await DbxSyncCommand.ExecuteAsync(null);
            else
                await SaveFilesCommand.ExecuteAsync(null);
        });
    }

    private void OnTwigPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender == _twig)
        {
            if (args.PropertyName == nameof(Core.TaskTwig.AutoSync))
            {
                AutoSync = _twig.AutoSync;
            }
        }
    }

    private void OnSleepPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender == _twig.SleepValues)
        {
            if (args.PropertyName == nameof(SleepValuesBacking.SleepStart))
            {
                IsSleeping = _twig.SleepValues.IsSleeping;
            }
        }
    }

    private void OnTodayChanged(object? sender, PropertyChangedEventArgs args)
    {
        _twig.TodaysJournal();
    }
    
    private void JournalsOnCollectionChanged(IChangeSet<Journal, DateOnly> obj)
    {
        Dispatcher.UIThread.Post(() =>
        {
            UpdateJournalBlackoutDates();
            OnJournalSelectedDateChanged(JournalSelectedDate);
        });
    }
    
    private void DbxHandlerOnAccountChanged(object? sender, DbxAccountChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            IsDbxConnected = e.IsAccountConnected;
            DbxAccountName = _twig.DbxHandler.GetAccountName();
            DbxPhoto = _getDbxPhoto(_twig.DbxHandler.GetAccountPhotoUri());
        });
    }

    private void CacheValidChanged(object? sender, CacheValidChangedEventArgs args)
    {
        CacheValid = args.IsAllCacheValid;
        InSync &= args.IsAllCacheValid;
    }

    public async Task Cleanup()
    {
        await _twig.SaveDataFiles();
    }
}