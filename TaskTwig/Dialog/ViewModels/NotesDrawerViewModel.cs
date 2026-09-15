using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TaskTwig.ViewModels;

namespace TaskTwig.Dialog.ViewModels;

public partial class NotesDrawerViewModel(MainViewModel vm) : ViewModelBase
{
    public MainViewModel ViewModel { get; } = vm;

    [ObservableProperty]
    public partial bool IsEditingList { get; set; } = false;
    
    [RelayCommand]
    private void StartEditingList() => IsEditingList = true;

    [RelayCommand]
    private void StopEditingList() => IsEditingList = false;
}