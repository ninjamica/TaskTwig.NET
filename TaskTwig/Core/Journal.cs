using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class Journal : ObservableObject
{
    public static string GlobalJournal { get; set; } = "";

    public required DateOnly Date { get; init; }
    public string JournalText { get; set; } = "";
    
}