namespace TaskTwig.Core;

public record Journal
{
    public static string GlobalText { get; set; } = "";
    public string Text { get; set; } = "";
    
}