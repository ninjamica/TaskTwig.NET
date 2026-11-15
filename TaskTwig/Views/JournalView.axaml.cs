using System;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Data.Converters;
using TaskTwig.ViewModels;

namespace TaskTwig.Views;

public partial class JournalView : UserControl
{
    public JournalView()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        TopLevel.GetTopLevel(this)?.InputPane?.StateChanged += InputPaneStateChanged;
        
        if (DataContext is MainViewModel mainViewModel)
            mainViewModel.JournalBlackoutDates = JournalCalendar.BlackoutDates;
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
            
            var journalInputPaneTop = topLevel?.TranslatePoint(e.EndRect.TopLeft, JournalGroupBox)?.Y;
            double journalNewMargin = 30;
            if (journalInputPaneTop is not null)
            {
                journalNewMargin += Math.Max(JournalGroupBox.Bounds.Height - journalInputPaneTop.Value, 0) + 30;
            }
            JournalGroupBox.Margin = new Thickness(30, 30, 30, journalNewMargin);
        }
        else
        {
            if (JournalGroupBox.Transitions?.FirstOrDefault() is ThicknessTransition journalTransition)
            {
                journalTransition.Duration = e.AnimationDuration;
                journalTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            JournalGroupBox.Margin = new Thickness(30);
        }
    }

    public static readonly FuncValueConverter<DateOnly?, string> JournalDateConverter =
        new(date => date?.ToString("dddd MMMM d") ?? "Select Journal");
}