using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using ObservableCollections;
using TaskTwig.Core.TwigInterval;
using Color = System.Drawing.Color;

namespace TaskTwig.Core;

public partial class TaskTwig : ObservableObject
{
    
    /// <summary>
    /// The time at which the next day starts, it must be on or after midnight and should be in the morning.
    /// </summary>
    public static TimeSpan DayStart
    {
        get;
        set
        {
            field = value;
            Today = EffectiveDate(DateTime.Now);
        }
    } = new TimeSpan(5, 0, 0);

    /// <summary>
    /// The current effective date. If the current time is after midnight but before <c>DayStart</c>,
    /// the value will be of the day before.
    /// </summary>
    public static DateOnly Today { get; private set; } = EffectiveDate(DateTime.Now);

    /// <summary>
    /// Calculates the effective date of a timestamp (where the day only starts after <c>DayStart</c>.
    /// </summary>
    /// <param name="dateTime">Timestamp to calculate date from</param>
    /// <returns></returns>
    public static DateOnly EffectiveDate(DateTime dateTime)
    {
        DateOnly date = DateOnly.FromDateTime(dateTime);
        
        if (dateTime.TimeOfDay.CompareTo(DayStart) < 0)
            date = date.AddDays(-1);
        
        return date;
    }
    
    
    // Containers for storing data in a way that's directly serializable
    struct SleepValues()
    {
        public ObservableDictionary<DateOnly, Sleep> SleepRecords { get; set; } = [];
        public DateTime? SleepStart { get; set; }
    }

    public partial class JournalValues : ObservableObject
    {
        public ObservableDictionary<DateOnly, Journal> Journals { get; set; } = [];
        [ObservableProperty] public partial string GlobalJournal { get; set; }
    }

    
    
    private SleepValues _sleepValues;
    public JournalValues JournalRecords { get; private set; } = new();

    public ObservableCollection<TaskCategory> TaskCategories
    {
        get;
        private set
        {
            field = value;
            DoneTodayTaskLists = new ObservableCollectionList<TwigTask, ReadOnlyObservableCollection<TwigTask>>(
                new MappedObservableList<TaskCategory, ReadOnlyObservableCollection<TwigTask>>(value,
                    category => category.DoneTodayTasks));
        }
    } = [];
    public ReadOnlyObservableCollection<TwigTask> DoneTodayTaskLists { get; private set; }

    public ObservableDictionary<DateOnly, Sleep> SleepRecords => _sleepValues.SleepRecords;
    public ObservableCollection<Exercise> Exercises { get; private set; } = [];
    public ObservableCollection<Workout> WorkoutList { get; private set; } = [];

    [ObservableProperty]
    public partial bool IsSleeping { get; private set; }

    private static readonly string _dataFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), 
        "TaskTwig-NET");

    private readonly DbxHandler _dbx;

    
    public TaskTwig()
    {
        if (!Directory.Exists(_dataFilePath))
            Directory.CreateDirectory(_dataFilePath);
        
        _dbx = new DbxHandler(_dataFilePath);
        
        Console.WriteLine(_dataFilePath);
        
        ReadDataFiles();
    }
    
    public void StartSleeping(DateTime sleepStart)
    {
        _SetSleepStart(sleepStart);
    }

    public bool FinishSleeping(DateTime sleepEnd, bool force)
    {
        DateOnly endDate = DateOnly.FromDateTime(sleepEnd).AddDays(-1);
        
        if (!force && SleepRecords.ContainsKey(endDate))
            return false;
        
        if (_sleepValues.SleepStart is { } sleepStart)
        {
            SleepRecords[endDate] = new Sleep(sleepStart, sleepEnd);
            _SetSleepStart(null);
            return true;
        }

        return false;
    }

    public Journal TodaysJournal()
    {
        if (!JournalRecords.Journals.ContainsKey(Today))
            JournalRecords.Journals[Today] = new Journal();

        return JournalRecords.Journals[Today];
    }

    public void WriteDataFiles()
    {
        string taskText = JsonSerializer.Serialize(TaskCategories);
        File.WriteAllText(Path.Combine(_dataFilePath, "task.json"), taskText);
        
        string sleepText = JsonSerializer.Serialize(_sleepValues);
        File.WriteAllText(Path.Combine(_dataFilePath, "sleep.json"), sleepText);
        
        string exerciseText = JsonSerializer.Serialize(Exercises);
        File.WriteAllText(Path.Combine(_dataFilePath, "exercise.json"), exerciseText);
        
        string workoutText = JsonSerializer.Serialize(WorkoutList);
        File.WriteAllText(Path.Combine(_dataFilePath, "workout.json"), workoutText);
        
        string journalText = JsonSerializer.Serialize(JournalRecords);
        File.WriteAllText(Path.Combine(_dataFilePath, "journal.json"), journalText);
        
        // string globalJournalText = JsonSerializer.Serialize(Journal.GlobalJournal);
        // File.WriteAllText(Path.Combine(_dataFilePath, "global-journal.json"), globalJournalText);
        
    }

    public void ReadDataFiles()
    {
        try
        {
            string taskText = File.ReadAllText(Path.Combine(_dataFilePath, "task.json"));
            TaskCategories = JsonSerializer.Deserialize<ObservableCollection<TaskCategory>>(taskText) ?? [];
            foreach (var category in TaskCategories)
            {
                foreach (var task in category.Tasks)
                {
                    task.Category = category;
                }
            }
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("task.json file not found");
            Console.WriteLine(e);
        }
        catch (JsonException e)
        {
            Console.WriteLine("failed to parse task.json");
            Console.WriteLine(e);
        }
        
        try
        {
            string sleepText = File.ReadAllText(Path.Combine(_dataFilePath, "sleep.json"));
            _sleepValues = JsonSerializer.Deserialize<SleepValues>(sleepText);
            _SetSleepStart(_sleepValues.SleepStart);
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("sleep.json file not found");
            Console.WriteLine(e);
        }
        catch (JsonException e)
        {
            Console.WriteLine("failed to parse sleep.json");
            Console.WriteLine(e);
        }
        
        try
        {
            string exerciseText = File.ReadAllText(Path.Combine(_dataFilePath, "exercise.json"));
            Exercises = JsonSerializer.Deserialize<ObservableCollection<Exercise>>(exerciseText) ?? [];
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("exercise.json file not found");
            Console.WriteLine(e);
        }
        catch (JsonException e)
        {
            Console.WriteLine("failed to parse exercise.json");
            Console.WriteLine(e);
        }
        
        try
        {
            string workoutText = File.ReadAllText(Path.Combine(_dataFilePath, "workout.json"));
            WorkoutList = JsonSerializer.Deserialize<ObservableCollection<Workout>>(workoutText) ?? [];
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("workout.json file not found");
            Console.WriteLine(e);
        }
        catch (JsonException e)
        {
            Console.WriteLine("failed to parse workout.json");
            Console.WriteLine(e);
        }
        
        try
        {
            string journalText = File.ReadAllText(Path.Combine(_dataFilePath, "journal.json"));
            JournalRecords = JsonSerializer.Deserialize<JournalValues>(journalText) ?? new JournalValues();
            
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("journal.json file not found");
            Console.WriteLine(e);
        }
        catch (JsonException e)
        {
            Console.WriteLine("failed to parse journal.json");
            Console.WriteLine(e);
        }
    }

    public static void Main()
    {
        TaskTwig twig = new();
        
        if (!twig._dbx.IsAccountConnected)
            twig._dbx.AuthFromUrlConsole();

        using (var stream = File.OpenRead(Path.Combine(_dataFilePath, "task.json")))
            twig._dbx.UploadFileAsync(stream, "/task.json").Wait();

        using (var fileStream = File.OpenWrite(Path.Combine(_dataFilePath, "test_task.json")))
            twig._dbx.DownloadFileAsync(fileStream, "/task.json").Wait();
        
        twig.WriteDataFiles();
    }

    private void _SetSleepStart(DateTime? dateTime)
    {
        _sleepValues.SleepStart = dateTime;
        IsSleeping = dateTime is not null;
    }
}