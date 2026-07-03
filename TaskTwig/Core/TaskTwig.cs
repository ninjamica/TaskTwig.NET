using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ObservableCollections;
using TaskTwig.Core.TwigInterval;
using Color = System.Drawing.Color;

namespace TaskTwig.Core;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DataFile
{
    Task,
    Sleep,
    Exercise,
    Workout,
    Journal,
    Note
}
    
public readonly struct DataFilePaths(string dataFileDir, string filename, string extension)
{
    public string LocalPath { get; } = Path.Combine(dataFileDir, $"{filename}.{extension}");
    public string BackupPath { get; } = Path.Combine(dataFileDir, $"{filename}_backup.{extension}");
    public string DbxPath { get; } = $"/{filename}.{extension}";
}

public struct HashCommit()
{
    public int Schema { get; init; } = 1;
    public byte[] OverallHash { get; set; } = []; 
    public ConcurrentDictionary<DataFile, byte[]> FileHashes { get; init; } = new();
}

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
    public ObservableCollectionList<TwTask, ReadOnlyObservableCollection<TwTask>> DoneTodayTaskLists { get; }

    public ObservableDictionary<DateOnly, Sleep> SleepRecords => _sleepValues.SleepRecords;
    public ObservableCollection<Exercise> Exercises { get; } = [];
    
    public ObservableCollection<Workout> WorkoutList { get; } = [];
    public ObservableDictionary<DateOnly, Journal> Journals { get; init; } = [];
    public ObservableCollection<Note> Notes { get; init; } = [];

    [ObservableProperty] public partial bool IsSleeping { get; private set; }

    private static readonly string _dataFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), 
        "TaskTwig-NET");
    
    public Dictionary<DataFile, DataFilePaths> DataFiles { get; } = new()
    {
        { DataFile.Task, new(_dataFilePath, "task", "json") },
        { DataFile.Sleep, new(_dataFilePath, "sleep", "json") },
        { DataFile.Exercise, new(_dataFilePath, "exercise", "json") },
        { DataFile.Workout, new(_dataFilePath, "workout", "json") },
        { DataFile.Journal, new(_dataFilePath, "journal", "json") },
        { DataFile.Note, new(_dataFilePath, "note", "json") }
    };
    
    public readonly DataFilePaths CommitFile = new(_dataFilePath, "commit", "json");
    public HashCommit Hashes { get; private set; } = new();

    public readonly DbxHandler DbxHandler;

    
    public TaskTwig()
    {
        DoneTodayTaskLists = new ObservableCollectionList<TwTask, ReadOnlyObservableCollection<TwTask>>(
            new MappedObservableList<TaskCategory, ReadOnlyObservableCollection<TwTask>>(
                TaskCategories, category => category.DoneTodayTasks));
        
        if (!Directory.Exists(_dataFilePath))
            Directory.CreateDirectory(_dataFilePath);
        
        DbxHandler = new DbxHandler(_dataFilePath);
    }
    
    public void StartSleeping(DateTime sleepStart)
    {
        _SetSleepStart(sleepStart);
    }

    public bool FinishSleeping(DateTime sleepEnd, bool overwrite)
    {

        if (_sleepValues.SleepStart is null)
            return false;

        var sleep = new Sleep(_sleepValues.SleepStart.Value, sleepEnd);
        
        if (!overwrite && SleepRecords.ContainsKey(sleep.Date))
            return false;
        
        SleepRecords[sleep.Date] = sleep;
        _SetSleepStart(null);
        return true;

    }

    public Journal TodaysJournal()
    {
        if (!Journals.ContainsKey(Today))
            Journals[Today] = new Journal();

        return Journals[Today];
    }
    
    public async Task ReadDataFiles()
    {
        var tasks = Enum.GetValues<DataFile>().Select(ReadDataFile);
        await Task.WhenAll(tasks);
    }

    public async Task ReadDataFile(DataFile file)
    {
        string jsonText = await File.ReadAllTextAsync(DataFiles[file].LocalPath);

        try
        {
            switch (file)
            {
                case DataFile.Task:
                    _ReadTasks(jsonText);
                    break;
                case DataFile.Sleep:
                    _ReadSleep(jsonText);
                    break;
                case DataFile.Exercise:
                    _ReadExercise(jsonText);
                    break;
                case DataFile.Workout:
                    _ReadWorkoutList(jsonText);
                    break;
                case DataFile.Journal:
                    _ReadJournal(jsonText);
                    break;
                case DataFile.Note:
                    _ReadNote(jsonText);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(file), file, null);
            }
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine($"{file.ToString()} not found at location {DataFiles[file].LocalPath}");
        }
        catch (JsonException e)
        {
            await _BackupDataFile(file);
            Console.WriteLine($"Failed to parse {file.ToString()} with the following error:");
            Console.WriteLine(e);
        }
    }

    private async Task _ReadLocalHashes()
    {
        string jsonText = await File.ReadAllTextAsync(CommitFile.LocalPath);
        Hashes = JsonSerializer.Deserialize<HashCommit>(jsonText);
    }

    private async Task<HashCommit> _DownloadDbxHashes()
    {
        await using var downloadStream = await DbxHandler.DownloadContentStreamAsync(CommitFile.DbxPath);
        return JsonSerializer.Deserialize<HashCommit>(downloadStream);
    }

    public async Task WriteDataFiles()
    {
        // await _HashLiveData();
        await _WriteDataFiles();
    }

    private async Task _WriteDataFiles()
    {
        var tasks = Enum.GetValues<DataFile>().Select(_WriteDataFile);
        await Task.WhenAll(tasks);
        await _WriteLocalHashes();
    }

    private async Task _WriteDataFile(DataFile file)
    {
        string jsonText = file switch
        {
            DataFile.Task => JsonSerializer.Serialize(TaskCategories),
            DataFile.Sleep => JsonSerializer.Serialize(_sleepValues),
            DataFile.Exercise => JsonSerializer.Serialize(Exercises),
            DataFile.Workout => JsonSerializer.Serialize(WorkoutList),
            DataFile.Journal => JsonSerializer.Serialize(Journals),
            DataFile.Note => JsonSerializer.Serialize(Notes),
            _ => throw new ArgumentOutOfRangeException(nameof(file), file, null)
        };
        
        await File.WriteAllTextAsync(DataFiles[file].LocalPath, jsonText);
    }

    private async Task _WriteLocalHashes()
    {
        string jsonText = JsonSerializer.Serialize(Hashes);
        await File.WriteAllTextAsync(CommitFile.LocalPath, jsonText);
    }
    
    public async Task BackupFiles()
    {
        await WriteDataFiles();
        Console.WriteLine("Done WriteDataFiles");

        var tasks = Enum.GetValues<DataFile>().Select(_BackupDataFile);
        await Task.WhenAll(tasks.Append(_BackupLocalHashes()));
        Console.WriteLine("Done Backup");
    }

    private async Task _BackupDataFile(DataFile file)
    {
        var filePaths = DataFiles[file];
        
        if (!File.Exists(filePaths.BackupPath))
            await File.Create(filePaths.BackupPath).DisposeAsync();
        
        File.Copy(filePaths.LocalPath, filePaths.BackupPath, true);
    }

    private async Task _BackupLocalHashes()
    {
        if (!File.Exists(CommitFile.BackupPath))
            await File.Create(CommitFile.BackupPath).DisposeAsync();
        
        File.Copy(CommitFile.LocalPath, CommitFile.BackupPath, true);
    }

    private async Task _HashLiveData()
    {
        var hashTasks = Enum.GetValues<DataFile>().Select(
            file => new Task(() =>
            {
                try
                {
                    Hashes.FileHashes[file] = _HashDataFile(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }));

        await Task.WhenAll(hashTasks);
    }

    private byte[] _HashDataFile(DataFile dataFile)
    {
        return dataFile switch
        {
            DataFile.Task => _HashTasks(),
            DataFile.Sleep => _hashSleep(),
            DataFile.Exercise => _hashExercise(),
            DataFile.Workout => _hashWorkout(),
            DataFile.Journal => _hashJournal(),
            DataFile.Note => _hashNote(),
            _ => throw new ArgumentOutOfRangeException(nameof(dataFile), dataFile, null)
        };
    }

    public async Task PushDbx()
    {
        await WriteDataFiles();

        var tasks = DataFiles.Values.Select(file =>
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
        
        var tasks = DataFiles.Values.Select(file =>
        {
            var stream = File.OpenWrite(file.LocalPath);
            return DbxHandler.DownloadFileAsync(stream, file.DbxPath);
        });
        
        await Task.WhenAll(tasks);
        Console.WriteLine("Done Pull");
        
        await ReadDataFiles();
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
    
    
    
    private void _ReadTasks(string jsonText)
    {
        var taskCategories = JsonSerializer.Deserialize<List<TaskCategory>>(jsonText) ?? [];
            
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

    private void _ReadSleep(string jsonText)
    {
        var sleepValues = JsonSerializer.Deserialize<SleepValues>(jsonText);
            
        _sleepValues.SleepRecords.Clear();
        foreach (var sleep in sleepValues.SleepRecords)
        {
            _sleepValues.SleepRecords[sleep.Key] = sleep.Value;
        }
            
        _SetSleepStart(sleepValues.SleepStart);
    }

    private void _ReadExercise(string jsonText)
    {
        var exercises = JsonSerializer.Deserialize<List<Exercise>>(jsonText) ?? [];

        Exercises.Clear();
        foreach (var exercise in exercises)
            Exercises.Add(exercise);
    }

    private void _ReadWorkoutList(string jsonText)
    {
        var workoutList = JsonSerializer.Deserialize<List<Workout>>(jsonText) ?? [];
            
        WorkoutList.Clear();
        foreach (var workout in workoutList)
            WorkoutList.Add(workout);
    }

    private void _ReadJournal(string jsonText)
    {
        var journalRecords = JsonSerializer.Deserialize<Dictionary<DateOnly, Journal>>(jsonText) ?? [];
            
        Journals.Clear();
            
        foreach (var journal in journalRecords)
            Journals[journal.Key] = journal.Value;
    }

    private void _ReadNote(string jsonText)
    {
        var noteRecords = JsonSerializer.Deserialize<List<Note>>(jsonText) ?? [];

        Notes.Clear();
            
        foreach (var note in noteRecords)
            Notes.Add(note);
    }
    
    private byte[] _HashTasks()
    {
        var hashAlgorithm = new XxHash3();

        foreach (var taskCategory in TaskCategories) 
            taskCategory.AppendHash(hashAlgorithm);

        return hashAlgorithm.GetCurrentHash();
    }

    private byte[] _hashSleep()
    {
        var hashAlgorithm = new XxHash3();

        hashAlgorithm.Append( _sleepValues.SleepStart switch
        {
            { } sleepStart => BitConverter.GetBytes(sleepStart.ToBinary()),
            _ => "null"u8
        });

        foreach (var sleepPair in SleepRecords.OrderBy(pair => pair.Key))
        {
            hashAlgorithm.Append(BitConverter.GetBytes(sleepPair.Key.DayNumber));
            sleepPair.Value.AppendHash(hashAlgorithm);
        }
        
        return hashAlgorithm.GetCurrentHash();
    }

    private byte[] _hashExercise()
    {
        var hashAlgorithm = new XxHash3();

        foreach (var exercise in Exercises) 
            exercise.AppendHash(hashAlgorithm);
        
        return hashAlgorithm.GetCurrentHash();
    }

    private byte[] _hashWorkout()
    {
        var hashAlgorithm = new XxHash3();
        
        foreach (var workout in WorkoutList)
            workout.AppendHash(hashAlgorithm);
        
        return hashAlgorithm.GetCurrentHash();
    }

    private byte[] _hashJournal()
    {
        var hashAlgorithm = new XxHash3();

        foreach (var journalPair in Journals.OrderBy(pair => pair.Key))
        {
            hashAlgorithm.Append(BitConverter.GetBytes(journalPair.Key.DayNumber));
            journalPair.Value.AppendHash(hashAlgorithm);
        }
        
        return hashAlgorithm.GetCurrentHash();
    }
    
    private byte[] _hashNote()
    {
        var hashAlgorithm = new XxHash3();
        
        foreach (var note in Notes)
            note.AppendHash(hashAlgorithm);
        
        return hashAlgorithm.GetCurrentHash();
    }
}