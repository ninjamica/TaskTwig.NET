using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
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

    public TaskTwig()
    {
        
    }

    public void WriteDataFiles()
    {
        string taskText = JsonSerializer.Serialize(TaskCategories);
        File.WriteAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/task.json", taskText);
        
        string sleepText = JsonSerializer.Serialize(SleepRecords);
        File.WriteAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/sleep.json", sleepText);
        
        string exerciseText = JsonSerializer.Serialize(Exercises);
        File.WriteAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/exercise.json", exerciseText);
        
        string workoutText = JsonSerializer.Serialize(WorkoutList);
        File.WriteAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/workout.json", workoutText);
        
        string journalText = JsonSerializer.Serialize(JournalRecords);
        File.WriteAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/journal.json", journalText);
    }

    public void ReadDataFiles()
    {
        string taskText = File.ReadAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/task.json");
        TaskCategories = JsonSerializer.Deserialize<List<TaskCategory>>(taskText) ?? [];
        
        string sleepText = File.ReadAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/sleep.json");
        SleepRecords = JsonSerializer.Deserialize<Dictionary<DateOnly, Sleep>>(sleepText) ?? [];
        
        string exerciseText = File.ReadAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/exercise.json");
        Exercises = JsonSerializer.Deserialize<List<Exercise>>(exerciseText) ?? [];
        
        string workoutText = File.ReadAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/workout.json");
        WorkoutList = JsonSerializer.Deserialize<List<Workout>>(workoutText) ?? [];
        
        string journalText = File.ReadAllText("/home/mike/RiderProjects/TaskTwig/TaskTwig/journal.json");
        JournalRecords = JsonSerializer.Deserialize<Dictionary<DateOnly, Journal>>(journalText) ?? [];
    }

    public static void Main()
    {
        TaskTwig twig = new();
        
        // twig.TaskCategories.Add(new TaskCategory
        // {
        //     Name = "test",
        //     Color = Color.Red
        // });
        //
        // twig.TaskCategories[0].AddTask(new Task
        // {
        //     Name = "task1",
        //     Category = twig.TaskCategories[0],
        //     Points = 5
        // });
        // twig.TaskCategories[0].AddTask(new Task
        // {
        //     Name = "task2",
        //     Category = twig.TaskCategories[0],
        //     Points = 10
        // });
        //
        // twig.TaskCategories[0].Tasks[0].SubTasks.Add(new SubTask()
        // {
        //     Name = "SubTask1",
        //     ParentTask = twig.TaskCategories[0].Tasks[0]
        // });
        //
        // twig.TaskCategories[0].Tasks[0].SubTasks.Add(new SubTask()
        // {
        //     Name = "SubTask2",
        //     ParentTask = twig.TaskCategories[0].Tasks[0]
        // });
        //
        // twig.SleepRecords.Add(DateOnly.FromDateTime(DateTime.Today), new Sleep()
        // {
        //     StartTime = DateTime.Now, 
        //     EndTime = DateTime.Now
        // });
        //
        // twig.Exercises.Add(new Exercise()
        // {
        //     Name = "push-up",
        //     Unit = Exercise.ExerciseUnit.Count
        // });
        // Console.WriteLine(twig.Exercises[0].ToString());
        //
        // twig.WorkoutList.Add(new Workout()
        // {
        //     StartTime = DateTime.Now,
        //     EndTime = DateTime.Now,
        //     Exercises =
        //     {
        //         [twig.Exercises[0]] = 5
        //     }
        // });
        //
        // twig.JournalRecords.Add(DateOnly.FromDateTime(DateTime.Today), new Journal()
        // {
        //     Text = "Testing journal"
        // });
        twig.ReadDataFiles();
        
        twig.WriteDataFiles();
    }
}