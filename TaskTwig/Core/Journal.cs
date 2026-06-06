using System;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.Core;

public partial class Journal : ObservableObject
{
    public string JournalText { get; set; } = "";
}