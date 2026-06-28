using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class Journal : ObservableObject
{
    
    [ObservableProperty]
    public partial string Text { get; set; } = "";
}