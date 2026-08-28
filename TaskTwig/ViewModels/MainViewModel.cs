using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Kernel;
using ObservableCollections;
using Sortable.Avalonia;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;
using TaskTwig.Views;
using Ursa.Common;
using Ursa.Controls;
using Ursa.Controls.Options;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace TaskTwig.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public WindowNotificationManager? NotificationManager { get; set; }
    private readonly Core.TaskTwig _twig;
    

    [ObservableProperty]
    public partial bool TodayDoneExpanded { get; set; } = true;

    private readonly ReadOnlyObservableCollection<TaskCategory> _taskCategoriesView;
    public ReadOnlyObservableCollection<TaskCategory> TaskCategoriesView => _taskCategoriesView;
    
    private readonly ReadOnlyObservableCollection<TwTask> _doneTodaytasks;
    public ReadOnlyObservableCollection<TwTask> DoneTodayTasks => _doneTodaytasks;

    private readonly ReadOnlyObservableCollection<Sleep> _sleepList;
    public ReadOnlyObservableCollection<Sleep> SleepList => _sleepList;
    
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
        var dialogViewModel = new TaskCategoryDialogViewModel(category);
        OverlayDrawer.ShowCustomAsync<TaskCategoryDialog, TaskCategoryDialogViewModel, bool>(dialogViewModel, options: drawerOptions)
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
        category.Tasks.Add(task);
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
        var dialogViewModel = new TaskDialogViewModel(task);
        OverlayDrawer.ShowCustomAsync<TaskDialog, TaskDialogViewModel, bool>(dialogViewModel, options:drawerOptions)
            .ContinueWith(result =>
            {
                if (result.Result)
                    task.Category?.Tasks.Remove(task);
            });
    }
    
    [RelayCommand]
    private void CategoryListUpdate(SortableUpdateEventArgs args)
    {
        args.ApplyUpdateMutation();
    }
    
    [RelayCommand]
    private void TaskListUpdate(SortableUpdateEventArgs args)
    {
        args.ApplyUpdateMutation();
    }

    [RelayCommand]
    private void TaskListDrop(SortableDropEventArgs args)
    {
        args.IsAccepted = true;
        args.TransferMode = SortableTransferMode.Move;
        args.ApplyDropMutation();
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
    
    public ObservableCollection<Note> Notes { get; set; }
    [ObservableProperty] public partial Note? SelectedNote { get; set; }

    [RelayCommand]
    private void CreateNote()
    {
        var newNote = new Note { Title = "New Note" };
        _twig.Notes.Add(newNote);
        SelectedNote = newNote;
    }

    [RelayCommand]
    private void DeleteNote(Note note)
    {
        _twig.Notes.Remove(note);
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PushDbxCommand), nameof(PullDbxCommand), nameof(DbxSyncCommand))]
    public partial bool IsDbxConnected { get; set; } = false;

    [ObservableProperty]
    public partial string? DbxAccountName { get; private set; }

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
            NotificationManager?.Show(new Notification("Saving Completed", string.Join(',', files)),
                NotificationType.Success);
        }
        catch (TwigInvalidOperationException)
        {
            NotificationManager?.Show(new Notification("Save Canceled", "Data operation already in progress"),
                NotificationType.Warning);
        }
    }

    [RelayCommand(CanExecute = nameof(IsDbxConnected))]
    private async Task PushDbx()
    {
        try
        {
            HashableObject.StopSaveTimer();
            await _twig.PushDbx();
            NotificationManager?.Show("Pushed To Dropbox", NotificationType.Success);
        }
        catch (TwigInvalidOperationException)
        {
            NotificationManager?.Show(new Notification("Push Canceled", "Data operation already in progress"),
                NotificationType.Warning);
        }
    }

    [RelayCommand(CanExecute = nameof(IsDbxConnected))]
    private async Task PullDbx()
    {
        try
        {
            HashableObject.StopSaveTimer();
            await _twig.PullDbx();
            NotificationManager?.Show("Pulled From Dropbox", NotificationType.Success);
        }
        catch (TwigInvalidOperationException)
        {
            NotificationManager?.Show(new Notification("Pull Canceled", "Data operation already in progress"),
                NotificationType.Warning);
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
            var notifTitle = new TextBlock
            {
                Text = "Syncing",
                FontSize = 16,
                FontWeight = FontWeight.SemiBold,
            };
            var notifContent = new TextBlock
            {
                Classes = { "Secondary" }
            };
            var loadingCircle = new LoadingIcon();
            var notifGrid = new Grid
            {
                Children = { notifTitle, notifContent, loadingCircle },
                RowDefinitions = new RowDefinitions("Auto, Auto"),
                ColumnDefinitions = new ColumnDefinitions("Auto, Auto"),
                ColumnSpacing = 10
            };
            Grid.SetRow(notifTitle, 0);
            Grid.SetColumn(notifTitle, 1);
            Grid.SetRow(notifContent, 1);
            Grid.SetColumn(notifContent, 1);
            Grid.SetRow(loadingCircle, 0);
            Grid.SetColumn(loadingCircle, 0);

            NotificationManager?.Show(notifGrid, NotificationType.Information, expiration: TimeSpan.Zero,
                showIcon: false);

            var progress = new Progress<SyncProgress>(syncProgress =>
            {
                switch (syncProgress.Stage)
                {
                    case SyncProgressStage.Hash:
                        notifTitle.Text = "Hashing Files";
                        notifContent.Text = null;
                        break;

                    case SyncProgressStage.Save:
                        notifTitle.Text = "Saving Files";

                        notifContent.Text = syncProgress.Files is { } files && files.Any()
                            ? string.Join(", ", files)
                            : "Nothing to save";
                        break;

                    case SyncProgressStage.Compare:
                        notifTitle.Text = "Comparing Files To Cloud";
                        notifContent.Text = null;
                        break;

                    case SyncProgressStage.Sync:
                        notifTitle.Text = "Syncing Files";

                        notifContent.Text = syncProgress.SyncActions is { Count: > 0 } actions
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
            // NotificationManager?.Close(notifGrid);
            NotificationManager?.CloseAll();

            if (actions is null)
            {
                NotificationManager?.Show(new Notification("Sync Canceled", null), NotificationType.Warning,
                    classes: ["Light"]);
            }
            else
            {
                NotificationManager?.Show(new Notification("Sync Completed", notifContent.Text),
                    NotificationType.Success, classes: ["Light"]);
            }
        }
        catch (TwigInvalidOperationException)
        {
            NotificationManager?.Show(new Notification("Sync Canceled", "Data operation already in progress"),
                NotificationType.Warning);
        }
    }
    
    [ObservableProperty]
    public partial TimeSpan DayStart { get; set; } = Core.TaskTwig.DayStart;
    partial void OnDayStartChanged(TimeSpan value) => Core.TaskTwig.DayStart = value;

    public MainViewModel()
    {
        _twig = new Core.TaskTwig();
        _twig.SleepValues.PropertyChanged += OnSleepPropertyChanged;
        Core.TaskTwig.OnTodayChanged += OnTodayChanged;
        _twig.DbxHandler.AccountChanged += DbxHandlerOnAccountChanged;
        
        _twig.InitDataFromFiles().ContinueWith(_ =>
        {
            if (SelectedNote != null && _twig.Notes.Count > 0 && !_twig.Notes.Contains(SelectedNote))
                SelectedNote = _twig.Notes.First();
            
            _twig.Journals.Connect().Subscribe(JournalsOnCollectionChanged);
            JournalSelectedDate = _twig.TodaysJournal().Date.ToDateTime(TimeOnly.MinValue);
            Dispatcher.UIThread.Post(UpdateJournalBlackoutDates);
            HashableObject.SaveCallback = async () =>
            {
                // DbxSyncCommand.Execute(null);
                await SaveFilesCommand.ExecuteAsync(null);
            };
        });
        
        _twig.TaskCategories.Connect().Bind(out _taskCategoriesView).Subscribe();
        _twig.TaskCategories.Connect()
            .MergeManyChangeSets(category => category.Tasks.Connect())
            .DisposeMany()
            .AutoRefresh()
            .AutoRefreshOnObservable(_ => Observable.FromEventPattern<PropertyChangedEventArgs>(handler => Core.TaskTwig.OnTodayChanged += handler, handler => Core.TaskTwig.OnTodayChanged -= handler))
            .Filter(task => task.LastDone.Equals(Core.TaskTwig.Today))
            .Bind(out _doneTodaytasks)
            .Subscribe();
        _twig.SleepValues.SleepRecords.Connect()
            .Bind(out _sleepList)
            .Subscribe();
        IsSleeping = _twig.SleepValues.IsSleeping;
        Notes = _twig.Notes;

        Task.Run(async () => await _twig.DbxHandler.AuthFromStoredKeys());
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

    public async Task Cleanup()
    {
        await _twig.SaveDataFiles();
    }
}