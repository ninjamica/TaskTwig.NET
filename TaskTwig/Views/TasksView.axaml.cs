using Avalonia;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;

namespace TaskTwig.Views;

public partial class TasksView : UserControl
{
    public static readonly DirectProperty<TasksView, bool> IsReorderCategoriesProperty =
        AvaloniaProperty.RegisterDirect<TasksView, bool>(nameof(IsReorderCategories), view => view.IsReorderCategories);
    
    public bool IsReorderCategories
    {
        get;
        private set => SetAndRaise(IsReorderCategoriesProperty, ref field, value);
    } = false;
    
    public TasksView()
    {
        InitializeComponent();
    }
    
    [RelayCommand]
    private void EnterCategoryReorder()
    {
        IsReorderCategories = true;
    }

    [RelayCommand]
    private void ExitCategoryReorder()
    {
        IsReorderCategories = false;
    }
}