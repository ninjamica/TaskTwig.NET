using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class Journal : ObservableObject
{
    // TODO: make observable (probably manually)
    public static string GlobalText { get; set; } = "";
    
    [ObservableProperty]
    public partial string Text { get; set; } = "";
    
}