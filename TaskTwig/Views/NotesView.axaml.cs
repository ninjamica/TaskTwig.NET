using System;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Markup.Xaml;

namespace TaskTwig.Views;

public partial class NotesView : UserControl
{
    public NotesView()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        TopLevel.GetTopLevel(this)?.InputPane?.StateChanged += InputPaneStateChanged;
    }

    private void InputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        if (e.NewState == InputPaneState.Open)
        {
            var topLevel = TopLevel.GetTopLevel(this);
    
            if (NotesGroupBox.Transitions?.FirstOrDefault() is ThicknessTransition notesTransition)
            {
                notesTransition.Duration = e.AnimationDuration;
                notesTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            var notesInputPaneTop = topLevel?.TranslatePoint(e.EndRect.TopLeft, NotesGroupBox)?.Y;
            double notesNewMargin = 30;
            if (notesInputPaneTop is not null)
            {
                notesNewMargin += Math.Max(NotesGroupBox.Bounds.Height - notesInputPaneTop.Value, 0) + 30;
            }
            NotesGroupBox.Margin = new Thickness(30, 30, 30, notesNewMargin);
        }
        else
        {
            if (NotesGroupBox.Transitions?.FirstOrDefault() is ThicknessTransition notesTransition)
            {
                notesTransition.Duration = e.AnimationDuration;
                notesTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            NotesGroupBox.Margin = new Thickness(30);
        }
    }
}