using System;
using System.Collections.Generic;
using Avalonia.Media;

namespace TaskTwig.Core;

public class TaskTwig
{
    public List<TaskCategory> TaskCategories { get; } = [];
    public Dictionary<DateOnly, Sleep> SleepRecords { get; } = new();
    public List<Exercise> Exercises { get; } = [];
    public List<Workout> WorkoutList { get; } = [];
    public Dictionary<DateOnly, Journal> JournalRecords { get; } = new();

    public TaskTwig()
    {
        TaskCategories.Add(new TaskCategory
        {
            Name = "test",
            Color = Color.FromRgb(255, 0, 0)
        });

        TaskCategories[0].AddTask(new Task
        {
            Name = "task",
            Category = TaskCategories[0],
            Points = 5
        });
        
        SleepRecords.Add(DateOnly.FromDateTime(DateTime.Today), new Sleep()
        {
            StartTime = DateTime.Today, 
            EndTime = DateTime.Today
        });
        
        Exercises.Add(new Exercise()
        {
            Name = "push-up",
            Unit = Exercise.ExerciseUnit.Count
        });
        
        WorkoutList.Add(new Workout()
        {
            StartTime = DateTime.Today,
            EndTime = DateTime.Today,
            Exercises =
            {
                [Exercises[0]] = 5
            }
        });
        
        JournalRecords.Add(DateOnly.FromDateTime(DateTime.Today), new Journal()
        {
            Text = "Testing journal"
        });
    }
}