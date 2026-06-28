using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
    } = new(5, 0, 0);

    /// <summary>
    /// The current effective date. If the current time is after midnight but before <c>DayStart</c>,
    /// the value will be of the day before.
    /// </summary>
    public static DateOnly Today { get; private set; } = EffectiveDate(DateTime.Now);

    /// <summary>
    /// Calculates the effective date of a timestamp (where the day only starts after <c>DayStart</c>).
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
        public ObservableDictionary<DateOnly, Sleep> SleepRecords { get; init; } = [];
        public DateTime? SleepStart { get; set; }
    }
    
    private SleepValues _sleepValues = new();

    public ObservableCollection<TaskCategory> TaskCategories { get; } = [];
    public ObservableCollectionList<TwigTask, ReadOnlyObservableCollection<TwigTask>> DoneTodayTaskLists { get; }

    public ObservableDictionary<DateOnly, Sleep> SleepRecords => _sleepValues.SleepRecords;
    public ObservableCollection<Exercise> Exercises { get; } = [];
    
    public ObservableCollection<Workout> WorkoutList { get; } = [];
    public ObservableDictionary<DateOnly, Journal> Journals { get; init; } = [];
    public ObservableCollection<Note> Notes { get; init; } = [];

    [ObservableProperty] public partial bool IsSleeping { get; private set; }

    private static readonly string _dataFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), 
        "TaskTwig-NET");
    
    public readonly struct DataFile(string filename, string extension)
    {
        public string LocalPath { get; } = Path.Combine(_dataFilePath, $"{filename}.{extension}");
        public string BackupPath { get; } = Path.Combine(_dataFilePath, $"{filename}_backup.{extension}");
        public string DbxPath { get; } = $"/{filename}.{extension}";
    }
    
    public DataFile[] DataFiles { get; } = [
        new("task", "json"),
        new("sleep", "json"),
        new("exercise", "json"),
        new("workout", "json"),
        new("journal", "json"),
        new("note", "json")
    ];

    public readonly DbxHandler DbxHandler;

    
    public TaskTwig()
    {
        DoneTodayTaskLists = new ObservableCollectionList<TwigTask, ReadOnlyObservableCollection<TwigTask>>(
            new MappedObservableList<TaskCategory, ReadOnlyObservableCollection<TwigTask>>(
                TaskCategories, category => category.DoneTodayTasks));
        
        if (!Directory.Exists(_dataFilePath))
            Directory.CreateDirectory(_dataFilePath);
        
        DbxHandler = new DbxHandler(_dataFilePath);
        
        ReadDataFiles();
    }
    
    public void StartSleeping(DateTime sleepStart)
    {
        _SetSleepStart(sleepStart);
    }

    public bool FinishSleeping(DateTime sleepEnd, bool overwrite)
    {

        if (_sleepValues.SleepStart is null)
            return false;
        
        DateOnly endDate = DateOnly.FromDateTime(sleepEnd).AddDays(-1);
        if (!overwrite && SleepRecords.ContainsKey(endDate))
            return false;
        
        SleepRecords[endDate] = new Sleep(_sleepValues.SleepStart.Value, sleepEnd);
        _SetSleepStart(null);
        return true;

    }

    public Journal TodaysJournal()
    {
        if (!Journals.ContainsKey(Today))
            Journals[Today] = new Journal();

        return Journals[Today];
    }

    public async Task WriteDataFiles()
    {
        string taskText = JsonSerializer.Serialize(TaskCategories);
        await File.WriteAllTextAsync(Path.Combine(_dataFilePath, "task.json"), taskText);
        
        string sleepText = JsonSerializer.Serialize(_sleepValues);
        await File.WriteAllTextAsync(Path.Combine(_dataFilePath, "sleep.json"), sleepText);
        
        string exerciseText = JsonSerializer.Serialize(Exercises);
        await File.WriteAllTextAsync(Path.Combine(_dataFilePath, "exercise.json"), exerciseText);
        
        string workoutText = JsonSerializer.Serialize(WorkoutList);
        await File.WriteAllTextAsync(Path.Combine(_dataFilePath, "workout.json"), workoutText);
        
        string journalText = JsonSerializer.Serialize(Journals);
        await File.WriteAllTextAsync(Path.Combine(_dataFilePath, "journal.json"), journalText);
        
        string noteText = JsonSerializer.Serialize(Notes);
        await File.WriteAllTextAsync(Path.Combine(_dataFilePath, "note.json"), noteText);
    }

    public void ReadDataFiles()
    {
        try
        {
            string taskText = File.ReadAllText(Path.Combine(_dataFilePath, "task.json"));
            var taskCategories = JsonSerializer.Deserialize<List<TaskCategory>>(taskText) ?? [];
            
            TaskCategories.Clear();
            foreach (var category in taskCategories)
            {
                TaskCategories.Add(category);
                foreach (var task in category.Tasks)
                {
                    task.Category = category;
                }
            }
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("task.json file not found");
        }
        catch (JsonException e)
        {
            File.Copy(Path.Combine(_dataFilePath, "task.json"), Path.Combine(_dataFilePath, "task_backup.json"), true);
            Console.WriteLine("failed to parse task.json");
            Console.WriteLine(e);
        }
        
        try
        {
            string sleepText = File.ReadAllText(Path.Combine(_dataFilePath, "sleep.json"));
            var sleepValues = JsonSerializer.Deserialize<SleepValues>(sleepText);
            
            _sleepValues.SleepRecords.Clear();
            foreach (var sleep in sleepValues.SleepRecords)
            {
                _sleepValues.SleepRecords[sleep.Key] = sleep.Value;
            }
            
            _SetSleepStart(sleepValues.SleepStart);
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("sleep.json file not found");
        }
        catch (JsonException e)
        {
            File.Copy(Path.Combine(_dataFilePath, "sleep.json"), Path.Combine(_dataFilePath, "sleep_backup.json"), true);
            Console.WriteLine("failed to parse sleep.json");
            Console.WriteLine(e);
        }
        
        try
        {
            string exerciseText = File.ReadAllText(Path.Combine(_dataFilePath, "exercise.json"));
            var exercises = JsonSerializer.Deserialize<List<Exercise>>(exerciseText) ?? [];

            Exercises.Clear();
            foreach (var exercise in exercises)
                Exercises.Add(exercise);
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("exercise.json file not found");
        }
        catch (JsonException e)
        {
            File.Copy(Path.Combine(_dataFilePath, "exercise.json"), Path.Combine(_dataFilePath, "exercise_backup.json"), true);
            Console.WriteLine("failed to parse exercise.json");
            Console.WriteLine(e);
        }
        
        try
        {
            string workoutText = File.ReadAllText(Path.Combine(_dataFilePath, "workout.json"));
            var workoutList = JsonSerializer.Deserialize<List<Workout>>(workoutText) ?? [];
            
            WorkoutList.Clear();
            foreach (var workout in workoutList)
                WorkoutList.Add(workout);
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("workout.json file not found");
        }
        catch (JsonException e)
        {
            File.Copy(Path.Combine(_dataFilePath, "workout.json"), Path.Combine(_dataFilePath, "workout_backup.json"), true);
            Console.WriteLine("failed to parse workout.json");
            Console.WriteLine(e);
        }
        
        try
        {
            string journalText = File.ReadAllText(Path.Combine(_dataFilePath, "journal.json"));
            var journalRecords = JsonSerializer.Deserialize<Dictionary<DateOnly, Journal>>(journalText);
            
            Journals.Clear();
            
            foreach (var journal in journalRecords)
                Journals[journal.Key] = journal.Value;
            
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("journal.json file not found");
        }
        catch (JsonException e)
        {
            File.Copy(Path.Combine(_dataFilePath, "journal.json"), Path.Combine(_dataFilePath, "journal_backup.json"), true);
            Console.WriteLine("failed to parse journal.json");
            Console.WriteLine(e);
        }
        
        try
        {
            string journalText = File.ReadAllText(Path.Combine(_dataFilePath, "note.json"));
            var noteRecords = JsonSerializer.Deserialize<List<Note>>(journalText);

            Notes.Clear();
            
            foreach (var note in noteRecords)
                Notes.Add(note);
            
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine("note.json file not found");
        }
        catch (JsonException e)
        {
            File.Copy(Path.Combine(_dataFilePath, "note.json"), Path.Combine(_dataFilePath, "note_backup.json"), true);
            Console.WriteLine("failed to parse note.json");
            Console.WriteLine(e);
        }
    }

    public async Task BackupFiles()
    {
        await WriteDataFiles();
        Console.WriteLine("Done WriteDataFiles");

        foreach (var file in DataFiles)
        {
            if (!File.Exists(file.BackupPath))
                await File.Create(file.BackupPath).DisposeAsync();
            File.Copy(file.LocalPath, file.BackupPath, true);
            
        }
        Console.WriteLine("Done Backup");
    }

    public async Task PushDbx()
    {
        await WriteDataFiles();

        var tasks = DataFiles.Select(file =>
        {
            var stream = File.OpenRead(file.LocalPath);
            return DbxHandler.UploadFileAsync(stream, file.DbxPath);
        });
        
        await Task.WhenAll(tasks);
        Console.WriteLine("Done Push");
    }

    public async Task PullDbx()
    {
        await BackupFiles();
        
        var tasks = DataFiles.Select(file =>
        {
            var stream = File.OpenWrite(file.LocalPath);
            return DbxHandler.DownloadFileAsync(stream, file.DbxPath);
        });
        
        await Task.WhenAll(tasks);
        Console.WriteLine("Done Pull");
        
        ReadDataFiles();
        Console.WriteLine("Done ReadDataFiles");
    }

    private void _SetSleepStart(DateTime? dateTime)
    {
        _sleepValues.SleepStart = dateTime;
        IsSleeping = dateTime is not null;
    }
    
    public static void Main()
    {
        TaskTwig twig = new();
        
        if (!twig.DbxHandler.IsAccountConnected)
            twig.DbxHandler.AuthFromUrlConsole();

        // using (var stream = File.OpenRead(Path.Combine(_dataFilePath, "task.json")))
        //     twig._dbx.UploadFileAsync(stream, "/task.json").Wait();

        using (var fileStream = File.OpenWrite(Path.Combine(_dataFilePath, "task.json")))
            twig.DbxHandler.DownloadFileAsync(fileStream, "/task.json").Wait();
        
        // twig.WriteDataFiles();
    }
}