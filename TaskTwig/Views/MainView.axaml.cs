using System;
using System.Linq;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Input;

namespace TaskTwig.Views;

public partial class MainView : UserControl
{

    public MainView() => InitializeComponent();

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        TopLevel.GetTopLevel(this)?.InputPane?.StateChanged += InputPaneStateChanged;
        
        DialogHost.Transitions ??= [new ThicknessTransition {Property = MarginProperty, Duration = TimeSpan.Zero}];
        
    }

    private void InputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        if (e.NewState == InputPaneState.Open)
        {
            if (DialogHost.Transitions?.FirstOrDefault() is ThicknessTransition dialogTransition)
            {
                dialogTransition.Duration = e.AnimationDuration;
                dialogTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
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
            if (DialogHost.Transitions?.FirstOrDefault() is ThicknessTransition dialogTransition)
            {
                dialogTransition.Duration = e.AnimationDuration;
                dialogTransition.Easing = e.Easing as Easing ?? new LinearEasing();
            }
            
            DialogHost.Margin = new Thickness(0);
        }
    }

    private void SelectedPageChanged(object? sender, SelectionChangedEventArgs e)
    {
        // DrawerPage.IsOpen = false;
        // await MainPage.ReplaceAsync((Page)DrawerListBox.SelectedItem);
        MainSplitView.IsPaneOpen = false;
    }

    private void DrawerSwipeGesture(object? sender, SwipeGestureEventArgs e)
    {
        switch (e.SwipeDirection)
        {
            case SwipeDirection.Left:
                MainSplitView.IsPaneOpen = false;
                break;
            
            case SwipeDirection.Right:
                MainSplitView.IsPaneOpen = true;
                break;
        }
    }
}