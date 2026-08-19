using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservableCollections;
using Sortable.Avalonia;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;
using TaskTwig.Views;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace TaskTwig.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public WindowNotificationManager? NotificationManager { get; set; }
    private readonly Core.TaskTwig _twig;
    

    [ObservableProperty]
    public partial bool TodayDoneExpanded { get; set; } = true;

    public ObservableCollection<TaskCategory> TaskCategoriesView { get; set; }
    public ReadOnlyObservableCollection<TwTask> DoneTodayTasks { get; set; }

    public NotifyCollectionChangedSynchronizedViewList<KeyValuePair<DateOnly, Sleep>> SleepList { get; init; }
    
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
        var dialogOptions = new OverlayDialogOptions()
        {
            Mode = DialogMode.Info,
            CanLightDismiss = true,
            VerticalAnchor = VerticalPosition.Top,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
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
        var dialogOptions = new OverlayDialogOptions()
        {
            Mode = DialogMode.Info,
            CanLightDismiss = true,
            VerticalAnchor = VerticalPosition.Top,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };
        var dialogViewModel = new TaskDialogViewModel(task);
        OverlayDialog.ShowCustomAsync<TaskDialog, TaskDialogViewModel, bool>(dialogViewModel, options:dialogOptions)
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
    
    [ObservableProperty]
    public partial Journal? TodaysJournal { get; private set; }
    
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
    public partial string? DbxAccountName { get; set; }

    public bool IsDbxConnected() => _twig.DbxHandler.IsAccountConnected;
    
    [RelayCommand]
    private async Task DbxSignInOut()
    {
        if (IsDbxConnected())
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
                DbxAccountName = null;
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
                _twig.DbxHandler.AuthFromCode(oAuth, code);
            }

            DbxAccountName = await _twig.DbxHandler.GetAccountName();
        }
        
    }

    [RelayCommand]
    private async Task SaveFiles()
    {
        var files = await _twig.WriteDataFiles();
        NotificationManager?.Show(new Notification("Saving Completed", string.Join(',', files)),
            NotificationType.Success);
    }

    [RelayCommand(CanExecute = nameof(IsDbxConnected))]
    private async Task PushDbx()
    {
        await _twig.PushDbx();
    }

    [RelayCommand(CanExecute = nameof(IsDbxConnected))]
    private async Task PullDbx()
    {
        await _twig.PullDbx();
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
        // var notification = new Notification("Syncing", null);
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
        
        NotificationManager?.Show(notifGrid, NotificationType.Information, expiration:TimeSpan.Zero, showIcon:false);
        
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
                        ? string.Join(", ", actions.Select(pair => $"{pair.Key}{(pair.Value == DataFileAction.Download ? "↓" : "↑")}"))
                        : "Nothing to do";
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        });

        var actions = await _twig.SyncWithDbx(SyncConflictCallback, progress);
        // NotificationManager?.Close(notification);
        NotificationManager?.CloseAll();

        if (actions is null)
        {
            NotificationManager?.Show(new Notification("Sync Canceled", null), NotificationType.Warning, classes: ["Light"]);
        }
        else
        {
            NotificationManager?.Show(new Notification("Sync Completed", notifContent.Text), NotificationType.Success, classes: ["Light"]);
        }
        
    }

    public MainViewModel()
    {
        _twig = new Core.TaskTwig();
        _twig.PropertyChanged += OnTwigOnPropertyChanged;
        
        _twig.InitDataFromFiles().ContinueWith(_ =>
        {
            if (_twig.Notes.Count > 0)
                SelectedNote = _twig.Notes.First();
        });
        
        TaskCategoriesView = _twig.TaskCategories;
        DoneTodayTasks = _twig.DoneTodayTaskLists;
        SleepList = _twig.SleepRecords.ToNotifyCollectionChanged();
        IsSleeping = _twig.IsSleeping;
        Notes = _twig.Notes;

        Task.Run(async () =>
        {
            await _twig.DbxHandler.AuthFromStoredKeys();
            var accountName = await _twig.DbxHandler.GetAccountName();
            Dispatcher.UIThread.Post(() => DbxAccountName = accountName);
        });
    }

    private void OnTwigOnPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (sender == _twig)
        {
            if (args.PropertyName == nameof(_twig.IsSleeping))
            {
                IsSleeping = _twig.IsSleeping;
            }
            else if (args.PropertyName == nameof(_twig.TodaysJournal))
            {
                TodaysJournal = _twig.TodaysJournal;
            }
        }
    }

    public void Cleanup()
    {
        // _twig.WriteDataFiles();
    }
}