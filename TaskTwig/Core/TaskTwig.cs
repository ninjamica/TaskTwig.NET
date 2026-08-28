using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Hashing;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
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

// Container for storing sleep data in a way that's directly serializable
public partial class SleepValuesBacking : HashableObject
{
    [JsonConverter(typeof(SleepSourceCacheConverter))]
    public SourceCache<Sleep, DateOnly> SleepRecords { get; init; } = new(sleep => sleep.Date);
        
    [ObservableProperty]
    public partial DateTime? SleepStart { get; private set; }
        
    [JsonIgnore]
    public bool IsSleeping => SleepStart is not null;
    
    
    public void StartSleeping(DateTime sleepStart)
    {
        SleepStart = sleepStart;
    }

    public bool FinishSleeping(DateTime sleepEnd, bool overwrite)
    {

        if (SleepStart is null)
            return false;

        var sleep = new Sleep(SleepStart.Value, sleepEnd);
        
        if (!overwrite && SleepRecords.Lookup(sleep.Date).HasValue)
            return false;
        
        SleepRecords.AddOrUpdate(sleep);
        SleepStart = null;
        return true;

    }

    public void CancelSleep()
    {
        SleepStart = null;
    }

    public void SetFrom(SleepValuesBacking? other)
    {
        SleepRecords.Clear();
        SleepStart = other?.SleepStart;

        if (other is not null)
            SleepRecords.AddOrUpdate(other.SleepRecords.Items);
    }

    public class SleepSourceCacheConverter : JsonConverter<SourceCache<Sleep, DateOnly>>
    {
        public override SourceCache<Sleep, DateOnly>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var cache = new SourceCache<Sleep, DateOnly>(sleep => sleep.Date);

            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    var dict = JsonSerializer.Deserialize<Dictionary<DateOnly, Sleep>>(ref reader, options);
                    if (dict is not null)
                        cache.AddOrUpdate(dict.Values);
                    break;
                
                case JsonTokenType.StartArray:
                    var list = JsonSerializer.Deserialize<List<Sleep>>(ref reader, options);
                    if (list is not null)
                        cache.AddOrUpdate(list);
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
        
            return cache;
        }

        public override void Write(Utf8JsonWriter writer, SourceCache<Sleep, DateOnly> value, JsonSerializerOptions options)
        {
            var items = value.Items.ToList();
            JsonSerializer.Serialize(writer, items, options);
        }
    }
        
    protected override void AppendHash(NonCryptographicHashAlgorithm hashAlgorithm)
    {
        hashAlgorithm.Append(BitConverter.GetBytes(SleepStart?.ToBinary() ?? 0));
    }

    protected override void AppendHashableChildren(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        foreach (var sleepRecord in SleepRecords.Items.OrderBy(sleep => sleep.Date))
        {
            sleepRecord.AppendHashAndChildren(mainHasher, childHasher);
        }
    }
}

public class TwigInvalidOperationException()
    : InvalidOperationException("Attempt to run multiple data operations simultaneously, which is not allowed");

public class TaskTwig : ObservableObject
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
    
    
    private int _isDataOperation = 0;
    private void _BeginDataOperation()
    {
        // Mark data operation in progress, throw exception if there's already an operation in progress
        if (Interlocked.CompareExchange(ref _isDataOperation, 1, 0) == 1)
            throw new TwigInvalidOperationException();
    }

    private void _EndDataOperation()
    {
        Interlocked.CompareExchange(ref _isDataOperation, 0, 1);
    }
    

    public SourceList<TaskCategory> TaskCategories { get; } = new();
    public SleepValuesBacking SleepValues { get; } = new();
    public ObservableCollection<Exercise> Exercises { get; } = [];
    public ObservableCollection<Workout> Workouts { get; } = [];
    public SourceCache<Journal, DateOnly> Journals { get; } = new(journal => journal.Date);
    public ObservableCollection<Note> Notes { get; } = [];

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

    public Journal TodaysJournal()
    {
        var journal = Journals.Lookup(Today);
        if (!journal.HasValue)
        {
            Console.WriteLine("Creating new journal for Today");
            var newJournal = new Journal { Date = Today };
            Journals.AddOrUpdate(newJournal);
            return newJournal;
        }
        
        return journal.Value;
    }

    public async Task InitDataFromFiles()
    {
        Dispatcher.UIThread.VerifyAccess();
        try
        {
            _BeginDataOperation();

            await _ReadDataFiles();
            await Task.WhenAll(_ReadLocalHashes(), _ReadLastSyncedHashes());
        }
        finally
        {
            _EndDataOperation();
        }
    }

    private async Task _ReadDataFiles(IEnumerable<DataFile>? files = null)
    {
        // await Task.WhenAll((files ?? Enum.GetValues<DataFile>()).Select(_ReadDataFile));
        
        Dispatcher.UIThread.VerifyAccess();
        
        HashableObject.IsReadingData = true;
        
        foreach (var file in files ?? Enum.GetValues<DataFile>())
        {
            var jsonText = await File.ReadAllTextAsync(DataFiles[file].LocalPath);

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
                Console.WriteLine($"Failed to parse {file.ToString()} with the following error:");
                Console.WriteLine(e);
            }
            finally
            {
                await Dispatcher.Yield(DispatcherPriority.Background);
            }
        }
        
        HashableObject.IsReadingData = false;
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
            return await JsonSerializer.DeserializeAsync<HashCommit>(downloadStream);
        }
        catch (ApiException<DownloadError>)
        {
            Console.WriteLine("Failed to download dbx commit file");
            return null;
        }
    }

    private async Task _UploadDbxHashes(DataFilePaths file)
    {
        await using var stream = File.OpenRead(file.LocalPath);
        await DbxHandler.UploadFileAsync(stream, file.DbxPath);
    }

    public async Task<List<DataFile>> SaveDataFiles()
    {
        Dispatcher.UIThread.VerifyAccess();
        
        try
        {
            _BeginDataOperation();
            return await WriteDataFiles();
        }
        finally
        {
            _EndDataOperation();
        }
    }

    private async Task<List<DataFile>> WriteDataFiles(IProgress<SyncProgress>? progress = null)
    {
        progress?.Report(new SyncProgress(SyncProgressStage.Hash));
        await _HashLiveData();
        
        var fileHashes = await _ReadHashFile(CommitFile.LocalPath);
        var diffFiles = fileHashes is null ? _liveHashes.FileHashes.Keys.ToList() : _FindHashDiffs(_liveHashes, fileHashes).ToList();
        progress?.Report(new SyncProgress(SyncProgressStage.Save, files: diffFiles));
        
        foreach (var file in diffFiles)
        {
            string jsonText;
            switch (file)
            {
                case DataFile.Task:
                    jsonText = JsonSerializer.Serialize(TaskCategories.Items.ToList());
                    break;
                case DataFile.Sleep:
                    jsonText = JsonSerializer.Serialize(SleepValues);
                    break;
                case DataFile.Exercise:
                    jsonText = JsonSerializer.Serialize(Exercises);
                    break;
                case DataFile.Workout:
                    jsonText = JsonSerializer.Serialize(Workouts);
                    break;
                case DataFile.Journal:
                    var journals = Journals.KeyValues.Where(pair => !pair.Value.IsEmpty()).ToDictionary();
                    jsonText = JsonSerializer.Serialize(journals);
                    break;
                case DataFile.Note:
                    jsonText = JsonSerializer.Serialize(Notes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(file), file, null);
            }

            await File.WriteAllTextAsync(DataFiles[file].LocalPath, jsonText);
            await Dispatcher.Yield(DispatcherPriority.Background);
        }
        
        await _WriteLocalHashes(CommitFile);
        return diffFiles;
    }

    private async Task _WriteLocalHashes(DataFilePaths file)
    {
        var jsonText = JsonSerializer.Serialize(_liveHashes);
        await File.WriteAllTextAsync(file.LocalPath, jsonText);
    }

    private async Task<bool> _HashLiveData()
    {
        Dispatcher.UIThread.VerifyAccess();
        
        HashableObject.AllCacheValid = true;
        
        // await Task.Run(() => Parallel.ForEach(DataFiles.Keys, file => _liveHashes.FileHashes[file] = _HashDataFile(file)));

        foreach (var file in DataFiles.Keys)
        {
            _liveHashes.FileHashes[file] = _HashDataFile(file);
            await Dispatcher.Yield(DispatcherPriority.Background);
        }
        
        var hashAlgorithm = new XxHash3();
        foreach (var hash in _liveHashes.FileHashes.Values)
            hashAlgorithm.Append(hash);
        
        _liveHashes.OverallHash = hashAlgorithm.GetCurrentHash();
        return HashableObject.AllCacheValid;
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

    private Dictionary<DataFile, DataFileAction> _CompareRemoteHashes(HashCommit? remoteHashes)
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

    private async Task _PerformSyncTransactions(Dictionary<DataFile, DataFileAction> actions)
    {
        // Handle file upload/downloads in parallel thread pool
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
        
        // Report any exceptions
        if (syncTask.Exception is { } exceptions)
        {
            Console.Error.WriteLine(exceptions);
        }

        // Handle newly downloaded data, update hashes, etc
        if (actions.ContainsValue(DataFileAction.Download))
        {
            await _ReadDataFiles(actions.Where(filePair => filePair.Value == DataFileAction.Download)
                .Select(filePair => filePair.Key));
            await _HashLiveData();
        }
        
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
        Dispatcher.UIThread.VerifyAccess();

        try
        {
            _BeginDataOperation();

            await WriteDataFiles(progress);

            progress?.Report(new SyncProgress(SyncProgressStage.Compare));
            var remoteHashes = await _DownloadDbxHashes();
            var actions = _CompareRemoteHashes(remoteHashes);

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

            await _PerformSyncTransactions(actions);
            return actions;
        }
        finally
        {
            _EndDataOperation();
        }
    }

    public async Task PushDbx()
    {
        Dispatcher.UIThread.VerifyAccess();

        try
        {
            _BeginDataOperation();
            
            await WriteDataFiles();
            await _PerformSyncTransactions(DataFiles.Keys.ToDictionary(file => file, _ => DataFileAction.Upload));

            Console.WriteLine("Done Push");
        }
        finally
        {
            _EndDataOperation();
        }
    }

    public async Task PullDbx()
    {
        Dispatcher.UIThread.VerifyAccess();

        try
        {
            _BeginDataOperation();

            await _PerformSyncTransactions(DataFiles.Keys.ToDictionary(file => file, _ => DataFileAction.Download));
            Console.WriteLine("Done Pull");
        }
        finally
        {
            _EndDataOperation();
        }
    }
    
    
    private void _ReadTasks(string jsonText)
    {
        var taskCategories = JsonSerializer.Deserialize<List<TaskCategory>>(jsonText) ?? [];
            
        TaskCategories.Edit(list =>
        {
            list.Clear();
            foreach (var category in taskCategories)
            {
                list.Add(category);
                foreach (var task in category.Tasks.Items)
                {
                    task.Category = category;
                }
            }
        });
    }

    private void _ReadSleep(string jsonText)
    {
        var sleepValues = JsonSerializer.Deserialize<SleepValuesBacking>(jsonText);
        SleepValues.SetFrom(sleepValues);
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
            
        Workouts.Clear();
        foreach (var workout in workoutList)
            Workouts.Add(workout);
    }

    private void _ReadJournal(string jsonText)
    {
        var journalRecords = JsonSerializer.Deserialize<Dictionary<DateOnly, Journal>>(jsonText) ?? [];

        Dispatcher.UIThread.Post(() =>
        {
            Journals.Edit(updater =>
            {
                updater.Clear();
                
                foreach (var (_, journal) in journalRecords)
                    Journals.AddOrUpdate(journal);
            });

            TodaysJournal();
        });
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
        SleepValues.AppendHashAndChildren(mainHasher, childHasher);
        
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
        foreach (var workout in Workouts)
            workout.AppendHashAndChildren(mainHasher, childHasher);
        
        return mainHasher.GetCurrentHash();
    }

    private byte[] _HashJournal(NonCryptographicHashAlgorithm mainHasher, NonCryptographicHashAlgorithm childHasher)
    {
        foreach (var journal in Journals.Items.OrderBy(journal => journal.Date))
        {
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