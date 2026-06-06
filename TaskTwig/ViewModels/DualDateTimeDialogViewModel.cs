using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.ViewModels;

public partial class DualDateTimeDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial DateTime? StartDateTimeValue { get; set; }
    
    [ObservableProperty]
    public partial DateTime? EndDateTimeValue { get; set; }
}