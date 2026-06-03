using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskTwig.Core.TwigInterval;
using Color = System.Drawing.Color;

namespace TaskTwig.Core;

public class TaskTwig : ObservableObject
{
    public static TimeOnly DayStart
    {
        get;
        set
        {
            field = value;
            Today = _Today();
        }
    } = new TimeOnly(5, 0);

    public static DateOnly Today { get; private set; } = _Today();

    private static DateOnly _Today()
    {
        if (TimeOnly.FromDateTime(DateTime.Now).CompareTo(DayStart) < 0)
            return DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
        else
            return DateOnly.FromDateTime(DateTime.Today);
    }

    public ObservableCollection<TaskCategory> TaskCategories { get; private set; } = [];
    public ObservableCollection<Sleep> SleepRecords { get; private set; } = [];
    public ObservableCollection<Exercise> Exercises { get; private set; } = [];
    public ObservableCollection<Workout> WorkoutList { get; private set; } = [];
    public ObservableCollection<Journal> JournalRecords { get; private set; } = [];

    private string _dataFilePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.Create);

    public void WriteDataFiles()
    {
        string taskText = JsonSerializer.Serialize(TaskCategories);
        File.WriteAllText(Path.Combine(_dataFilePath, "task.json"), taskText);
        
        string sleepText = JsonSerializer.Serialize(SleepRecords);
        File.WriteAllText(Path.Combine(_dataFilePath, "sleep.json"), sleepText);
        
        string exerciseText = JsonSerializer.Serialize(Exercises);
        File.WriteAllText(Path.Combine(_dataFilePath, "exercise.json"), exerciseText);
        
        string workoutText = JsonSerializer.Serialize(WorkoutList);
        File.WriteAllText(Path.Combine(_dataFilePath, "workout.json"), workoutText);
        
        string journalText = JsonSerializer.Serialize(JournalRecords);
        File.WriteAllText(Path.Combine(_dataFilePath, "journal.json"), journalText);
        
        string globalJournalText = JsonSerializer.Serialize(Journal.GlobalJournal);
        File.WriteAllText(Path.Combine(_dataFilePath, "global-journal.json"), globalJournalText);
        
    }

    public void ReadDataFiles()
    {
        string taskText = File.ReadAllText(Path.Combine(_dataFilePath, "task.json"));
        TaskCategories = JsonSerializer.Deserialize<ObservableCollection<TaskCategory>>(taskText) ?? [];
        
        string sleepText = File.ReadAllText(Path.Combine(_dataFilePath, "sleep.json"));
        SleepRecords = JsonSerializer.Deserialize<ObservableCollection<Sleep>>(sleepText) ?? [];
        
        string exerciseText = File.ReadAllText(Path.Combine(_dataFilePath, "exercise.json"));
        Exercises = JsonSerializer.Deserialize<ObservableCollection<Exercise>>(exerciseText) ?? [];
        
        string workoutText = File.ReadAllText(Path.Combine(_dataFilePath, "workout.json"));
        WorkoutList = JsonSerializer.Deserialize<ObservableCollection<Workout>>(workoutText) ?? [];
        
        string journalText = File.ReadAllText(Path.Combine(_dataFilePath, "journal.json"));
        JournalRecords = JsonSerializer.Deserialize<ObservableCollection<Journal>>(journalText) ?? [];

        try
        {
            string globalJournalText = File.ReadAllText(Path.Combine(_dataFilePath, "global-journal.json"));
            Journal.GlobalJournal = JsonSerializer.Deserialize<string>(globalJournalText) ?? "";
        }
        catch (FileNotFoundException e)
        {
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
        
        twig.SleepRecords.Add(new Sleep()
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
        
        twig.JournalRecords.Add(new Journal
        {
            Date = TaskTwig.Today,
            JournalText = "Testing journal"
        });
        // twig.ReadDataFiles();
        
        twig.WriteDataFiles();
    }
}