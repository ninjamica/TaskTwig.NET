using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TaskTwig.ViewModels;

namespace TaskTwig.Dialog.ViewModels;

public partial class DateTimeDialogViewModel : ViewModelBase
{
    [ObservableProperty] public partial DateTime? DateTimeValue { get; set; } = DateTime.Now;
}