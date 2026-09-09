using TaskTwig.ViewModels;

namespace TaskTwig.Dialog.ViewModels;

public class NotesDrawerViewModel(MainViewModel vm) : ViewModelBase
{
    public MainViewModel ViewModel { get; } = vm;
}