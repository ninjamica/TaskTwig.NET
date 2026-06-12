using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TaskTwig.ViewModels;

public partial class DateTimeDialogViewModel : ViewModelBase
{
    [ObservableProperty] public partial DateTime? DateTimeValue { get; set; } = DateTime.Now;
}