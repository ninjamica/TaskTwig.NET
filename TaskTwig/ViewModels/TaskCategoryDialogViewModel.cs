using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using TaskTwig.Core;

namespace TaskTwig.ViewModels;

public partial class TaskCategoryDialogViewModel(TaskCategory category) : ViewModelBase, IDialogContext
{
    [ObservableProperty] 
    public partial TaskCategory Category { get; private set; } = category;
    
    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke(this, false);
    }

    [RelayCommand]
    public void Delete()
    {
        RequestClose?.Invoke(this, true);
    }
    
    public event EventHandler<object?>? RequestClose;
}