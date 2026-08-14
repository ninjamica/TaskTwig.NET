using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using TaskTwig.Core;
using TaskTwig.ViewModels;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace TaskTwig.Views;

public partial class MainView : UserControl
{
    
    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is not MainViewModel mainViewModel)
            return;

        var topLevel = TopLevel.GetTopLevel(this);
        mainViewModel.NotificationManager = WindowNotificationManager.TryGetNotificationManager(topLevel, out var manager)
            ? manager
            : new WindowNotificationManager(topLevel);
    }

    private void TodayNameClicked(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: TaskCategory category })
        {
            category.Expanded = !category.Expanded;
            Console.WriteLine(category.Expanded);
            Console.WriteLine(e.Source?.GetType());
        }
    }

    private void DoneNameClicked(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel mainViewModel)
        {
            mainViewModel.TodayDoneExpanded = !mainViewModel.TodayDoneExpanded;
        }
    }

    private void TaskDragStarted(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse)
        {
            Console.WriteLine("Starting drag");
            TasksScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }
    }

    private void TaskDragEnded(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse)
        {
            Console.WriteLine("Ending drag");
            TasksScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }
    }
    
    
    private void TaskScrollStarted(object? sender, ScrollGestureEventArgs e)
    {
        Console.WriteLine("Starting scroll");
        TasksScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
    }

    private void TaskScrollEnded(object? sender, ScrollGestureEndedEventArgs e)
    {
        Console.WriteLine("Ending scroll");
        TasksScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
    }
}