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
using Dropbox.Api;
using Dropbox.Api.Files;
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

public enum DataFileAction
{
    Download,
    Upload,
    Conflict
}
    
public readonly struct DataFilePaths(string dataFileDir, string filename, string extension)
{
    public string LocalPath { get; } = Path.Combine(dataFileDir, $"{filename}.{extension}");
    public string BackupPath { get; } = Path.Combine(dataFileDir, $"{filename}_backup.{extension}");
    public string DbxPath { get; } = $"/{filename}.{extension}";
}

public class HashCommit()
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

    private static readonly string DataDirPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create), 
        "TaskTwig-NET");
    
    public Dictionary<DataFile, DataFilePaths> DataFiles { get; } = new()
    {
        { DataFile.Task, new(DataDirPath, "task", "json") },
        { DataFile.Sleep, new(DataDirPath, "sleep", "json") },
        { DataFile.Exercise, new(DataDirPath, "exercise", "json") },
        { DataFile.Workout, new(DataDirPath, "workout", "json") },
        { DataFile.Journal, new(DataDirPath, "journal", "json") },
        { DataFile.Note, new(DataDirPath, "note", "json") }
    };
    
    public readonly DataFilePaths CommitFile = new(DataDirPath, "commit", "json");
    public readonly DataFilePaths DbxCommitFile = new(Path.Combine(DataDirPath, "dbx"), "commit", "json");
    public HashCommit Hashes { get; private set; } = new();

    public readonly DbxHandler DbxHandler;

    
    public TaskTwig()
    {
        DoneTodayTaskLists = new ObservableCollectionList<TwTask, ReadOnlyObservableCollection<TwTask>>(
            new MappedObservableList<TaskCategory, ReadOnlyObservableCollection<TwTask>>(
                TaskCategories, category => category.DoneTodayTasks));
        
        if (!Directory.Exists(DataDirPath))
            Directory.CreateDirectory(DataDirPath);
        
        DbxHandler = new DbxHandler(DataDirPath);
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
            Journals[Today] = new Journal { Date = Today };

        return Journals[Today];
    }
    
    public async Task ReadDataFiles()
    {
        // await Parallel.ForEachAsync(Enum.GetValues<DataFile>(), async (file, _) => await ReadDataFile(file));
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

    private async Task<HashCommit?> _ReadLocalHashes()
    {
        string jsonText = await File.ReadAllTextAsync(CommitFile.LocalPath);
        return JsonSerializer.Deserialize<HashCommit>(jsonText);
    }

    private async Task<HashCommit?> _ReadLastSyncedHashes()
    {
        try
        {
            // string jsonText = await File.ReadAllTextAsync(DbxCommitFile.LocalPath);
            return await JsonSerializer.DeserializeAsync<HashCommit>(File.OpenRead(DbxCommitFile.LocalPath));
        }
        catch (FileNotFoundException e)
        {
            Console.WriteLine($"{CommitFile.ToString()} not found at location {CommitFile.LocalPath}");
            return null;
        }
    } 

    private async Task<HashCommit?> _DownloadDbxHashes()
    {
        try
        {
            await using var downloadStream = await DbxHandler.DownloadContentStreamAsync(CommitFile.DbxPath);
            return JsonSerializer.Deserialize<HashCommit>(downloadStream);
        }
        catch (ApiException<DownloadError> e)
        {
            Console.WriteLine("Failed to download dbx commit file");
            return null;
        }
    }

    private async Task _UploadDbxHashes(DataFilePaths file)
    {
        var stream = File.OpenRead(file.LocalPath);
        await DbxHandler.UploadFileAsync(stream, file.DbxPath);
    }

    public async Task WriteDataFiles()
    {
        await _HashLiveData();
        await _WriteDataFiles();
    }

    private async Task _WriteDataFiles()
    {
        // var tasks = Enum.GetValues<DataFile>().Select(_WriteDataFile);
        // await Task.WhenAll(tasks);
        await Parallel.ForEachAsync(Enum.GetValues<DataFile>(), async (file, _) => await _WriteDataFile(file));
        await _WriteLocalHashes(CommitFile);
    }

    private async Task _WriteDataFile(DataFile file)
    {
        string jsonText;
        switch (file)
        {
            case DataFile.Task:
                jsonText = JsonSerializer.Serialize(TaskCategories);
                break;
            case DataFile.Sleep:
                jsonText = JsonSerializer.Serialize(_sleepValues);
                break;
            case DataFile.Exercise:
                jsonText = JsonSerializer.Serialize(Exercises);
                break;
            case DataFile.Workout:
                jsonText = JsonSerializer.Serialize(WorkoutList);
                break;
            case DataFile.Journal:
                foreach (var journalPair in Journals)
                {
                    if (journalPair.Key != Today && journalPair.Value.IsEmpty())
                        Journals.Remove(journalPair.Key);
                }
                jsonText = JsonSerializer.Serialize(Journals);
                break;
            case DataFile.Note:
                jsonText = JsonSerializer.Serialize(Notes);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(file), file, null);
        }

        await File.WriteAllTextAsync(DataFiles[file].LocalPath, jsonText);
    }

    private async Task _WriteLocalHashes(DataFilePaths file)
    {
        string jsonText = JsonSerializer.Serialize(Hashes);
        await File.WriteAllTextAsync(file.LocalPath, jsonText);
    }
    
    public async Task BackupFiles()
    {
        await WriteDataFiles();

        // var tasks = Enum.GetValues<DataFile>().Select(_BackupDataFile);
        // await Task.WhenAll(tasks.Append(_BackupLocalHashes()));
        await Parallel.ForEachAsync(Enum.GetValues<DataFile>(), async (file, _) => await _WriteDataFile(file));
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
        await Task.Run(() => Parallel.ForEach(DataFiles.Keys, file => Hashes.FileHashes[file] = _HashDataFile(file)));
        
        var hashAlgorithm = new XxHash3();
        foreach (var hash in Hashes.FileHashes.Values)
            hashAlgorithm.Append(hash);
        
        Hashes.OverallHash = hashAlgorithm.GetCurrentHash();
    }

    private byte[] _HashDataFile(DataFile dataFile)
    {
        var mainHasher = new XxHash3();
        var childHasher = new XxHash3();
        return dataFile switch
        {
            DataFile.Task => _HashTasks(mainHasher, childHasher),
            DataFile.Sleep => _HashSleep(mainHasher, childHasher),
            DataFile.Exercise => _HashExercise(mainHasher),
            DataFile.Workout => _HashWorkout(mainHasher, childHasher),
            DataFile.Journal => _HashJournal(mainHasher, childHasher),
            DataFile.Note => _HashNote(mainHasher, childHasher),
            _ => throw new ArgumentOutOfRangeException(nameof(dataFile), dataFile, null)
        };
    }

    public static Dictionary<DataFile, DataFileAction> CompareHashes(HashCommit local, HashCommit remote, HashCommit? lastSynced)
    {
        if (local.Schema != remote.Schema || (lastSynced is not null && local.Schema != lastSynced.Schema))
            throw new InvalidOperationException("Hash schema versions do not match (TODO: implement handling of this)");

        Dictionary<DataFile, DataFileAction> diffs = new();
        foreach (var file in local.FileHashes.Keys)
        {
            if (!local.FileHashes[file].SequenceEqual(remote.FileHashes[file]))
            {
                if (lastSynced is null)
                    diffs[file] = DataFileAction.Conflict;
                else if (local.FileHashes[file].SequenceEqual(lastSynced.FileHashes[file]))
                    diffs[file] = DataFileAction.Download;
                else if (remote.FileHashes[file].SequenceEqual(lastSynced.FileHashes[file]))
                    diffs[file] = DataFileAction.Upload;
                else
                    diffs[file] = DataFileAction.Conflict;
            }
        }

        return diffs;
    }

    public async Task PerformSyncTransactions(IDictionary<DataFile, DataFileAction> actions)
    {
        await Parallel.ForEachAsync(actions, async (filePair, _) =>
        {
            var file = DataFiles[filePair.Key];
            switch (filePair.Value)
            {
                case DataFileAction.Download:
                    var fileWriteStream = File.OpenWrite(file.LocalPath); 
                    await DbxHandler.DownloadFileAsync(fileWriteStream, file.DbxPath);
                    break;
                
                case DataFileAction.Upload:
                    var fileReadStream = File.OpenRead(file.LocalPath);
                    await DbxHandler.UploadFileAsync(fileReadStream, file.DbxPath);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException(nameof(filePair.Value), filePair.Value,
                        "Actions must only be Upload or Download");
            }
        });

        await ReadDataFiles();
        await _HashLiveData();
        await _WriteLocalHashes(CommitFile); 
        await _WriteLocalHashes(DbxCommitFile);
        await _UploadDbxHashes(DbxCommitFile);
    }

    public async Task<Dictionary<DataFile, DataFileAction>> SyncWithDbx(Func<Dictionary<DataFile, DataFileAction>, Task<Dictionary<DataFile, DataFileAction>>> conflictCallback)
    {
        await WriteDataFiles();
        var lastSynced = await _ReadLastSyncedHashes();
        var remote = await _DownloadDbxHashes();

        var actions = remote is null
            ? DataFiles.Keys.ToDictionary(file => file, _ => DataFileAction.Conflict)
            : CompareHashes(Hashes, remote, lastSynced);

        if (actions.ContainsValue(DataFileAction.Conflict)) 
            actions = await conflictCallback(actions);
        
        foreach (var dataFileAction in actions)
        {
            Console.WriteLine($"{dataFileAction.Key}: {dataFileAction.Value}");
        }
        
        await PerformSyncTransactions(actions);
        return actions;
    }

    public async Task PushDbx()
    {
        await WriteDataFiles();

        // var tasks = DataFiles.Values.Select(file =>
        // {
        //     var stream = File.OpenRead(file.LocalPath);
        //     return DbxHandler.UploadFileAsync(stream, file.DbxPath);
        // });
        //
        // await Task.WhenAll(tasks);

        await Parallel.ForEachAsync(DataFiles.Values, async (file, _) =>
        {
            var stream = File.OpenRead(file.LocalPath);
            await DbxHandler.UploadFileAsync(stream, file.DbxPath);
        });
        Console.WriteLine("Done Push");
    }

    public async Task PullDbx()
    {
        await BackupFiles();
        
        // var tasks = DataFiles.Values.Select(file =>
        // {
        //     var stream = File.OpenWrite(file.LocalPath);
        //     return DbxHandler.DownloadFileAsync(stream, file.DbxPath);
        // });
        //
        // await Task.WhenAll(tasks);

        await Parallel.ForEachAsync(DataFiles.Values, async (file, _) =>
        {
            var stream = File.OpenWrite(file.LocalPath); 
            await DbxHandler.DownloadFileAsync(stream, file.DbxPath);
        });
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

        using (var fileStream = File.OpenWrite(Path.Combine(DataDirPath, "task.json")))
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
    
    private byte[] _HashTasks(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        foreach (var taskCategory in TaskCategories) 
            taskCategory.AppendHashAndChildren(mainHasher, childHasher);

        return mainHasher.GetCurrentHash();
    }

    private byte[] _HashSleep(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        mainHasher.Append( _sleepValues.SleepStart switch
        {
            { } sleepStart => BitConverter.GetBytes(sleepStart.ToBinary()),
            _ => "null"u8
        });

        foreach (var sleepPair in SleepRecords.OrderBy(pair => pair.Key))
        {
            // mainHasher.Append(BitConverter.GetBytes(sleepPair.Key.DayNumber));
            sleepPair.Value.AppendHashAndChildren(mainHasher, childHasher);
        }
        
        return mainHasher.GetCurrentHash();
    }

    private byte[] _HashExercise(NonCryptographicHashAlgorithm mainHasher)
    {
        foreach (var exercise in Exercises) 
            exercise.AppendHash(mainHasher);
        
        return mainHasher.GetCurrentHash();
    }

    private byte[] _HashWorkout(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        foreach (var workout in WorkoutList)
            workout.AppendHashAndChildren(mainHasher, childHasher);
        
        return mainHasher.GetCurrentHash();
    }

    private byte[] _HashJournal(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        foreach (var journalPair in Journals.OrderBy(pair => pair.Key))
        {
            // mainHasher.Append(BitConverter.GetBytes(journalPair.Key.DayNumber));
            if (!journalPair.Value.IsEmpty())
                journalPair.Value.AppendHashAndChildren(mainHasher, childHasher);
        }
        
        return mainHasher.GetCurrentHash();
    }
    
    private byte[] _HashNote(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        foreach (var note in Notes)
            note.AppendHashAndChildren(mainHasher, childHasher);
        
        return mainHasher.GetCurrentHash();
    }
}