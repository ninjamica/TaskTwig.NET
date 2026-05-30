using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using TaskTwig.Core.TwigInterval;
using Color = System.Drawing.Color;

namespace TaskTwig.Core;

public class TaskTwig
{
    public static TimeOnly DayStart { get; set; } = new TimeOnly(5, 0);

    public static DateOnly Today
    {
        get
        {
            if (TimeOnly.FromDateTime(DateTime.Now).CompareTo(DayStart) < 0)
                return DateOnly.FromDateTime(DateTime.Today).AddDays(-1);
            else
                return DateOnly.FromDateTime(DateTime.Today);
        }
    }

    public List<TaskCategory> TaskCategories { get; private set; } = [];
    public Dictionary<DateOnly, Sleep> SleepRecords { get; private set; } = new();
    public List<Exercise> Exercises { get; private set; } = [];
    public List<Workout> WorkoutList { get; private set; } = [];
    public Dictionary<DateOnly, Journal> JournalRecords { get; private set; } = new();

    public void WriteDataFiles()
    {
        string taskText = JsonSerializer.Serialize(TaskCategories);
        File.WriteAllText("../../../task.json", taskText);
        
        string sleepText = JsonSerializer.Serialize(SleepRecords);
        File.WriteAllText("../../../sleep.json", sleepText);
        
        string exerciseText = JsonSerializer.Serialize(Exercises);
        File.WriteAllText("../../../exercise.json", exerciseText);
        
        string workoutText = JsonSerializer.Serialize(WorkoutList);
        File.WriteAllText("../../../workout.json", workoutText);
        
        string journalText = JsonSerializer.Serialize(JournalRecords);
        File.WriteAllText("../../../journal.json", journalText);
    }

    public void ReadDataFiles()
    {
        string taskText = File.ReadAllText("../../../task.json");
        TaskCategories = JsonSerializer.Deserialize<List<TaskCategory>>(taskText) ?? [];
        
        string sleepText = File.ReadAllText("../../../sleep.json");
        SleepRecords = JsonSerializer.Deserialize<Dictionary<DateOnly, Sleep>>(sleepText) ?? [];
        
        string exerciseText = File.ReadAllText("../../../exercise.json");
        Exercises = JsonSerializer.Deserialize<List<Exercise>>(exerciseText) ?? [];
        
        string workoutText = File.ReadAllText("../../../workout.json");
        WorkoutList = JsonSerializer.Deserialize<List<Workout>>(workoutText) ?? [];
        
        string journalText = File.ReadAllText("../../../journal.json");
        JournalRecords = JsonSerializer.Deserialize<Dictionary<DateOnly, Journal>>(journalText) ?? [];
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
        
        twig.SleepRecords.Add(DateOnly.FromDateTime(DateTime.Today), new Sleep()
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
        
        twig.JournalRecords.Add(DateOnly.FromDateTime(DateTime.Today), new Journal()
        {
            Text = "Testing journal"
        });
        // twig.ReadDataFiles();
        
        twig.WriteDataFiles();
    }
}