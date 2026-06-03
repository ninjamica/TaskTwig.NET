using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial string DailyJournal { get; set; }
    partial void OnDailyJournalChanged(string value) => _twig.JournalRecords[0].JournalText = value;
    
    [ObservableProperty]
    public partial string GlobalJournal { get; set; }

    public ObservableCollection<TaskCategory> TaskCategoryList { get; set; }

    partial void OnGlobalJournalChanged(string value) => Journal.GlobalJournal = value;

    private Core.TaskTwig _twig;

    [RelayCommand]
    public void CreateTaskCategory()
    {
        _twig.TaskCategories.Add(new TaskCategory());
    }

    [RelayCommand]
    public void CreateTask(TaskCategory category)
    {
        category.Tasks.Add(new Task()
        {
            Name = "New Task",
            Interval = new NoInterval()
        });
    }

    public MainViewModel()
    {
        _twig = new Core.TaskTwig();
        _twig.ReadDataFiles();
        
        DailyJournal = _twig.JournalRecords[0].JournalText;
        GlobalJournal = Journal.GlobalJournal;
        TaskCategoryList = _twig.TaskCategories;
    }

    public void Cleanup()
    {
        _twig.WriteDataFiles();
    }
}