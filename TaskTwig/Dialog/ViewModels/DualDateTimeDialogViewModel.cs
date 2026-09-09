using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskTwig.ViewModels;

namespace TaskTwig.Dialog.ViewModels;

public partial class DualDateTimeDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    public partial DateTime? StartDateTimeValue { get; set; }
    
    [ObservableProperty]
    public partial DateTime? EndDateTimeValue { get; set; }
}