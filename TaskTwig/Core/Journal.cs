
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class Journal : ObservableObject
{
    [ObservableProperty]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public partial string? Title { get; set; }
    
    [ObservableProperty]
    public partial string Text { get; set; } = "";
}