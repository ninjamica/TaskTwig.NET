using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using TaskTwig.Core;
using TaskTwig.ViewModels;

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

        mainViewModel.JournalBlackoutDates = JournalCalendar.BlackoutDates;
    }

    private void TodayNameClicked(object? sender, PointerPressedEventArgs e)
        
        TopLevel.GetTopLevel(this)?.InputPane?.StateChanged += InputPaneStateChanged;
        
        DialogHost.Transitions ??= [new ThicknessTransition {Property = MarginProperty, Duration = TimeSpan.Zero}];
    }


    private void InputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        if (e.NewState == InputPaneState.Open)
        {
            var topLevel = TopLevel.GetTopLevel(this);

            if (JournalGroupBox.Transitions?.FirstOrDefault() is ThicknessTransition journalTransition)
            {
                journalTransition.Duration = e.AnimationDuration;
                journalTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            if (NotesGroupBox.Transitions?.FirstOrDefault() is ThicknessTransition notesTransition)
            {
                notesTransition.Duration = e.AnimationDuration;
                notesTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            if (DialogHost.Transitions?.FirstOrDefault() is ThicknessTransition dialogTransition)
            {
                dialogTransition.Duration = e.AnimationDuration;
                dialogTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            var journalInputPaneTop = topLevel?.TranslatePoint(e.EndRect.TopLeft, JournalGroupBox)?.Y;
            double journalNewMargin = 30;
            if (journalInputPaneTop is not null)
            {
                journalNewMargin += Math.Max(JournalGroupBox.Bounds.Height - journalInputPaneTop.Value, 0) + 30;
            }
            JournalGroupBox.Margin = new Thickness(30, 30, 30, journalNewMargin);
            
            var notesInputPaneTop = topLevel?.TranslatePoint(e.EndRect.TopLeft, NotesGroupBox)?.Y;
            double notesNewMargin = 30;
            if (notesInputPaneTop is not null)
            {
                notesNewMargin += Math.Max(NotesGroupBox.Bounds.Height - notesInputPaneTop.Value, 0) + 30;
            }
            NotesGroupBox.Margin = new Thickness(30, 30, 30, notesNewMargin);
            
            var dialogInputPaneTop = TopLevel.GetTopLevel(this)?.TranslatePoint(e.EndRect.TopLeft, DialogHost)?.Y;
            double dialogNewMargin = 0;
            if (dialogInputPaneTop is not null)
            {
                dialogNewMargin += Math.Max(DialogHost.Bounds.Height - dialogInputPaneTop.Value, 0);
            }
            DialogHost.Margin = new Thickness(0, 0, 0, dialogNewMargin);
        }
        else
        {
            if (JournalGroupBox.Transitions?.FirstOrDefault() is ThicknessTransition journalTransition)
            {
                journalTransition.Duration = e.AnimationDuration;
                journalTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            if (NotesGroupBox.Transitions?.FirstOrDefault() is ThicknessTransition notesTransition)
            {
                notesTransition.Duration = e.AnimationDuration;
                notesTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            if (DialogHost.Transitions?.FirstOrDefault() is ThicknessTransition dialogTransition)
            {
                dialogTransition.Duration = e.AnimationDuration;
                dialogTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            JournalGroupBox.Margin = new Thickness(30);
            NotesGroupBox.Margin = new Thickness(30);
            DialogHost.Margin = new Thickness(0);
        }
    }

    {
        if (sender is Control { DataContext: TaskCategory category })
        {
            category.Expanded = !category.Expanded;
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
            Console.WriteLine("Starting Task drag");
            e.PreventGestureRecognition();
        }
    }

    public static readonly FuncValueConverter<bool, string> SleepButtonTextConverter =
        new(isSleeping => isSleeping ? "Wake Up" : "Go To Sleep");

    public static readonly FuncValueConverter<DateOnly?, string> JournalDateConverter =
        new(date => date?.ToString("dddd MMMM d") ?? "Select Journal");
    
    public static readonly FuncValueConverter<bool, string> AccountButtonTextConverter =
        new(connected => connected ? "Sign Out" : "Sign In");
}