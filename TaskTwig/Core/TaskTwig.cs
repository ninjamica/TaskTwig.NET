using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Avalonia.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using Dropbox.Api;
using Dropbox.Api.Files;
using DynamicData;
using ObservableCollections;
using WeakEvent;

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

public enum SyncProgressStage
{
    Hash,
    Save,
    Compare,
    Sync
}

public readonly struct SyncProgress(
    SyncProgressStage stage,
    IEnumerable<DataFile>? files = null,
    Dictionary<DataFile, DataFileAction>? syncActions = null)
{
    public SyncProgressStage Stage { get; } = stage;
    public IEnumerable<DataFile>? Files { get; } = files;
    public Dictionary<DataFile, DataFileAction>? SyncActions { get; } = syncActions;
}
    
public readonly struct DataFilePaths(string dataFileDir, string filename, string extension)
{
    public string LocalPath { get; } = Path.Combine(dataFileDir, $"{filename}.{extension}");
    public string BackupPath { get; } = Path.Combine(dataFileDir, $"{filename}_backup.{extension}");
    public string DbxPath { get; } = $"/{filename}.{extension}";
}

public class HashCommit
{
    public int Schema { get; init; } = 1;
    public byte[]? OverallHash { get; set; } 
    public ConcurrentDictionary<DataFile, byte[]> FileHashes { get; init; } = new();

    public void SetFrom(HashCommit? hashes)
    {
        FileHashes.Clear();
        if (hashes is null)
        {
            OverallHash = null;
        }
        else
        {
            OverallHash = hashes.OverallHash;
            foreach (var (file, hash) in hashes.FileHashes)
                FileHashes[file] = hash;
        }
    }
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
    public static DateOnly Today
    {
        get;
        private set
        {
            field = value;
            TodayChangedEventSource.Raise(null, new PropertyChangedEventArgs(nameof(Today)));
        }
    } = EffectiveDate(DateTime.Now);

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

    private static readonly WeakEventSource<PropertyChangedEventArgs> TodayChangedEventSource = new();
    public static event EventHandler<PropertyChangedEventArgs> OnTodayChanged
    {
        add => TodayChangedEventSource.Subscribe(value);
        remove => TodayChangedEventSource.Unsubscribe(value);
    }

    public static void RefreshToday()
    {
        Today = EffectiveDate(DateTime.Now);
    }
    
    
    // Containers for storing data in a way that's directly serializable
    struct SleepValues()
    {
        public ObservableDictionary<DateOnly, Sleep> SleepRecords { get; init; } = [];
        public DateTime? SleepStart { get; set; }
    }
    
    private SleepValues _sleepValues = new();

    public SourceList<TaskCategory> TaskCategories { get; } = new();

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
        { DataFile.Task,     new DataFilePaths(DataDirPath, "task",     "json") },
        { DataFile.Sleep,    new DataFilePaths(DataDirPath, "sleep",    "json") },
        { DataFile.Exercise, new DataFilePaths(DataDirPath, "exercise", "json") },
        { DataFile.Workout,  new DataFilePaths(DataDirPath, "workout",  "json") },
        { DataFile.Journal,  new DataFilePaths(DataDirPath, "journal",  "json") },
        { DataFile.Note,     new DataFilePaths(DataDirPath, "note",     "json") }
    };
    
    public readonly DataFilePaths CommitFile = new(DataDirPath, "commit", "json");
    public readonly DataFilePaths DbxCommitFile = new(Path.Combine(DataDirPath, "dbx"), "commit", "json");
    private readonly HashCommit _liveHashes = new();
    private readonly HashCommit _lastSyncedHashes = new();

    public readonly DbxHandler DbxHandler;

    
    public TaskTwig()
    {
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

    public async Task InitDataFromFiles()
    {
        await Task.WhenAll(ReadDataFiles(), _ReadLocalHashes(), _ReadLastSyncedHashes());
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
        catch (FileNotFoundException)
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

    private static async Task<HashCommit?> _ReadHashFile(string path)
    {
        await using var file = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<HashCommit>(file);
    }

    private async Task _ReadLocalHashes()
    {
        _liveHashes.SetFrom(await _ReadHashFile(CommitFile.LocalPath));
    }

    private async Task _ReadLastSyncedHashes()
    {
        try
        {
            _lastSyncedHashes.SetFrom(await _ReadHashFile(DbxCommitFile.LocalPath));
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine($"{CommitFile.ToString()} not found at location {CommitFile.LocalPath}");
            _lastSyncedHashes.SetFrom(null);
        }
    } 

    private async Task<HashCommit?> _DownloadDbxHashes()
    {
        try
        {
            await using var downloadStream = await DbxHandler.DownloadContentStreamAsync(CommitFile.DbxPath);
            return JsonSerializer.Deserialize<HashCommit>(downloadStream);
        }
        catch (ApiException<DownloadError>)
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

    public async Task<List<DataFile>> WriteDataFiles(IProgress<SyncProgress>? progress = null)
    {
        progress?.Report(new SyncProgress(SyncProgressStage.Hash));
        await _HashLiveData();
        
        var fileHashes = await _ReadHashFile(CommitFile.LocalPath);
        var diffFiles = fileHashes is null ? _liveHashes.FileHashes.Keys.ToList() : _FindHashDiffs(_liveHashes, fileHashes).ToList();
        progress?.Report(new SyncProgress(SyncProgressStage.Save, files: diffFiles));
        
        await Parallel.ForEachAsync(diffFiles, async (file, _) =>
        {
            Console.WriteLine($"Saving {file}");
            await _WriteDataFile(file);
        });
        await _WriteLocalHashes(CommitFile);
        return diffFiles;
    }

    private async Task _WriteDataFile(DataFile file)
    {
        string jsonText;
        switch (file)
        {
            case DataFile.Task:
                jsonText = JsonSerializer.Serialize(TaskCategories.Items.ToList());
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
                foreach (var (date, journal) in Journals)
                {
                    if (!date.Equals(Today) && journal.IsEmpty())
                        Journals.Remove(date);
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
        string jsonText = JsonSerializer.Serialize(_liveHashes);
        await File.WriteAllTextAsync(file.LocalPath, jsonText);
    }
    
    public async Task BackupFiles()
    {
        var files = await WriteDataFiles();

        // var tasks = Enum.GetValues<DataFile>().Select(_BackupDataFile);
        // await Task.WhenAll(tasks.Append(_BackupLocalHashes()));
        await Parallel.ForEachAsync(files, async (file, _) => await _BackupDataFile(file));
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
        await Task.Run(() => Parallel.ForEach(DataFiles.Keys, file => _liveHashes.FileHashes[file] = _HashDataFile(file)));
        
        var hashAlgorithm = new XxHash3();
        foreach (var hash in _liveHashes.FileHashes.Values)
            hashAlgorithm.Append(hash);
        
        _liveHashes.OverallHash = hashAlgorithm.GetCurrentHash();
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

    private static IEnumerable<DataFile> _FindHashDiffs(HashCommit hash1, HashCommit hash2)
    {
        return hash1.OverallHash.SequenceEqual(hash2.OverallHash)
            ? []
            : hash1.FileHashes.Keys.Union(hash2.FileHashes.Keys).Where(file =>
                hash1.FileHashes.ContainsKey(file) ^ hash2.FileHashes.ContainsKey(file) ||
                !hash1.FileHashes[file].SequenceEqual(hash2.FileHashes[file]));
    }

    public Dictionary<DataFile, DataFileAction> CompareRemoteHashes(HashCommit? remoteHashes)
    {
        if (remoteHashes is null)
            return DataFiles.Keys.ToDictionary(file => file, _ => DataFileAction.Conflict);
        
        if (_liveHashes.Schema != remoteHashes.Schema || _liveHashes.Schema != _lastSyncedHashes.Schema)
            throw new InvalidOperationException("Hash schema versions do not match (TODO: implement handling of this)");

        Dictionary<DataFile, DataFileAction> diffs = new();
        foreach (var file in _liveHashes.FileHashes.Keys.Union(_lastSyncedHashes.FileHashes.Keys)
                     .Union(remoteHashes.FileHashes.Keys))
        {
            _liveHashes.FileHashes.TryGetValue(file, out var liveHash);
            _lastSyncedHashes.FileHashes.TryGetValue(file, out var lastSyncedHash);
            remoteHashes.FileHashes.TryGetValue(file, out var remoteHash);

            if (remoteHash is null)
                diffs[file] = DataFileAction.Upload;
            
            else if (liveHash is null)
                diffs[file] = DataFileAction.Download;
            
            else if (!liveHash.SequenceEqual(remoteHash))
            {
                if (lastSyncedHash is null)
                    diffs[file] = DataFileAction.Conflict;
                else if (liveHash.SequenceEqual(lastSyncedHash))
                    diffs[file] = DataFileAction.Download;
                else if (remoteHash.SequenceEqual(lastSyncedHash))
                    diffs[file] = DataFileAction.Upload;
                else
                    diffs[file] = DataFileAction.Conflict;
            }
        }

        return diffs;
    }

    public async Task PerformSyncTransactions(Dictionary<DataFile, DataFileAction> actions)
    {
        var syncTask = Parallel.ForEachAsync(actions, async (filePair, _) =>
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
        await syncTask;
        if (syncTask.Exception is { } exceptions)
        {
            Console.Error.WriteLine(exceptions);
        }

        await ReadDataFiles();
        await _HashLiveData();
        await _WriteLocalHashes(CommitFile);
        
        _lastSyncedHashes.SetFrom(_liveHashes);
        await _WriteLocalHashes(DbxCommitFile);
        
        if (actions.ContainsValue(DataFileAction.Upload))
            await _UploadDbxHashes(DbxCommitFile);
    }

    public async Task<Dictionary<DataFile, DataFileAction>?> SyncWithDbx(
        Func<Dictionary<DataFile, DataFileAction>, Task<Dictionary<DataFile, DataFileAction>?>>? conflictCallback = null,
        IProgress<SyncProgress>? progress = null)
    {
        await WriteDataFiles(progress);
        
        progress?.Report(new SyncProgress(SyncProgressStage.Compare));
        var remoteHashes = await _DownloadDbxHashes();
        var actions = CompareRemoteHashes(remoteHashes);
        
        if (actions.ContainsValue(DataFileAction.Conflict))
        {
            if (conflictCallback is not null)
            {
                actions = await conflictCallback(actions);
                if (actions is null)
                    return null;
            }
            else
            {
                return null;
            }
        }
        
        progress?.Report(new SyncProgress(SyncProgressStage.Sync, syncActions: actions));
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

        using var fileStream = File.OpenWrite(Path.Combine(DataDirPath, "task.json"));
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
            foreach (var task in category.Tasks.Items)
            {
                task.Category = category;
            }
        }
    }

    private void _ReadSleep(string jsonText)
    {
        var sleepValues = JsonSerializer.Deserialize<SleepValues>(jsonText);
            
        _sleepValues.SleepRecords.Clear();
        foreach (var (date, sleep) in sleepValues.SleepRecords)
        {
            _sleepValues.SleepRecords[date] = sleep;
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
            
        foreach (var (date, journal) in journalRecords)
            Journals[date] = journal;
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
        foreach (var taskCategory in TaskCategories.Items) 
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

        foreach (var (_, sleep) in SleepRecords.OrderBy(pair => pair.Key))
        {
            // mainHasher.Append(BitConverter.GetBytes(sleepPair.Key.DayNumber));
            sleep.AppendHashAndChildren(mainHasher, childHasher);
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
        foreach (var (_, journal) in Journals.OrderBy(pair => pair.Key))
        {
            // mainHasher.Append(BitConverter.GetBytes(journalPair.Key.DayNumber));
            if (!journal.IsEmpty())
                journal.AppendHashAndChildren(mainHasher, childHasher);
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