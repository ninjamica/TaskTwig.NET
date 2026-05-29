namespace TaskTwig.Core;

public record Exercise()
{
    public enum ExerciseUnit
    {
        Count,
        Seconds,
        Minutes,
        Miles
    }
    
    public required string Name { get; set; }
    public required ExerciseUnit Unit { get; set; }
}