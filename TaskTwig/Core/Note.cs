using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class Note : ObservableObject
{
    [ObservableProperty] public required partial string Title { get; set; }
    [ObservableProperty] public partial string Text { get; set; } = "";
}