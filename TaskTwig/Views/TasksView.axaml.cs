using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    
    private void CategoryDragStarted(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse)
        {
            Console.WriteLine("Starting TaskCategory drag");
            e.PreventGestureRecognition();
        }
    }

    private void TaskDragStarted(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse)
        {
            Console.WriteLine("Starting Task drag");
            e.PreventGestureRecognition();
        }
    }
}