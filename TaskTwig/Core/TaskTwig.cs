using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public static TimeSpan DayStart
    {
        get;
        set
        {
            field = value;
            Today = EffectiveDate(DateTime.Now);
        }
    } = new TimeSpan(5, 0, 0);

    public static DateOnly Today { get; private set; } = EffectiveDate(DateTime.Now);

    public static DateOnly EffectiveDate(DateTime dateTime)
    {
        DateOnly date = DateOnly.FromDateTime(dateTime);
        
        if (dateTime.TimeOfDay.CompareTo(DayStart) < 0)
            date = date.AddDays(-1);
        
        return date;
    }

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

    public ObservableList<TaskCategory> TaskCategories { get; private set; } = [];
    public ObservableDictionary<DateOnly, Sleep> SleepRecords => _sleepValues.SleepRecords;
    public ObservableCollection<Exercise> Exercises { get; private set; } = [];
    public ObservableCollection<Workout> WorkoutList { get; private set; } = [];
    // public ObservableDictionary<DateOnly, Journal> JournalRecords => _journalValues.JournalRecords;

    [ObservableProperty]
    public partial bool IsSleeping { get; private set; }

    private readonly string _dataFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);
    
    
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
            TaskCategories = JsonSerializer.Deserialize<ObservableList<TaskCategory>>(taskText) ?? [];
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
        
        twig.TaskCategories.Add(new TaskCategory
        {
            Name = "test",
            Color = Color.Red
        });
        
        twig.TaskCategories[0].AddTask(new Task
        {
            Name = "task1",
            Category = twig.TaskCategories[0],
            Interval = new UnitInterval()
            {
                Unit = DateUnit.Day,
                UnitAmount = 2,
                AutoRepeat = false
            },
            OPattern = Task.OccurrencePattern.OccurOn,
            EPattern = Task.AutoExtendPattern.Auto,
            Points = 5
        });
        twig.TaskCategories[0].AddTask(new Task
        {
            Name = "task2",
            Category = twig.TaskCategories[0],
            Interval = new WeekInterval()
            {
                DayOfWeekMap = DayOfWeekFlag.Monday | DayOfWeekFlag.Friday,
                WeekSpacing = 2,
                AutoRepeat = false
            },
            OPattern = Task.OccurrencePattern.StartOn,
            EPattern = Task.AutoExtendPattern.FromCompletion,
            Points = 10
        });
        twig.TaskCategories[0].AddTask(new Task
        {
            Name = "task3",
            Category = twig.TaskCategories[0],
            Interval = new DailyInterval(),
            Points = 2
        });
        
        twig.TaskCategories[0].Tasks[0].SubTasks.Add(new SubTask()
        {
            Name = "SubTask1",
            ParentTask = twig.TaskCategories[0].Tasks[0]
        });
        
        twig.TaskCategories[0].Tasks[0].SubTasks.Add(new SubTask()
        {
            Name = "SubTask2",
            ParentTask = twig.TaskCategories[0].Tasks[0]
        });
        
        twig.SleepRecords.Add(Today, new Sleep()
        {
            StartTime = DateTime.Now, 
            EndTime = DateTime.Now
        });
        
        twig.Exercises.Add(new Exercise()
        {
            Name = "push-up",
            Unit = Exercise.ExerciseUnit.Count
        });
        Console.WriteLine(twig.Exercises[0].ToString());
        
        twig.WorkoutList.Add(new Workout()
        {
            StartTime = DateTime.Now,
            EndTime = DateTime.Now,
            Exercises =
            {
                [twig.Exercises[0]] = 5
            }
        });
        
        twig.JournalRecords.Journals.Add(Today, new Journal
        {
            JournalText = "Testing journal"
        });
        // twig.ReadDataFiles();
        
        twig.WriteDataFiles();
    }

    private void _SetSleepStart(DateTime? dateTime)
    {
        _sleepValues.SleepStart = dateTime;
        IsSleeping = dateTime is not null;
    }
}