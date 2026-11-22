using System;
using System.Collections.ObjectModel;
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
    public static readonly DirectProperty<MainView, ObservableCollection<ContentPage>> PagesProperty = AvaloniaProperty.RegisterDirect<MainView, ObservableCollection<ContentPage>>(
        nameof(Pages), o => o.Pages, (o, v) => o.Pages = v);

    public ObservableCollection<ContentPage> Pages
    {
        get;
        set => SetAndRaise(PagesProperty, ref field, value);
    } = [];

    public MainView() => InitializeComponent();

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        
        TopLevel.GetTopLevel(this)?.InputPane?.StateChanged += InputPaneStateChanged;
        
        DialogHost.Transitions ??= [new ThicknessTransition {Property = MarginProperty, Duration = TimeSpan.Zero}];
        
        if (Pages.Count == 0)
        {
            Pages.Add(new ContentPage
            {
                Header = "Today", Icon = Application.Current?.FindResource("SemiIconCalendar"),
                Content = new TodayView { DataContext = DataContext }
            });
            Pages.Add(new ContentPage
            {
                Header = "Tasks", Icon = Application.Current?.FindResource("SemiIconCheckList"),
                Content = new TasksView { DataContext = DataContext }
            });
            Pages.Add(new ContentPage
            {
                Header = "Sleep", Icon = Application.Current?.FindResource("SemiIconAlarm"),
                Content = new SleepView { DataContext = DataContext }
            });
            Pages.Add(new ContentPage
            {
                Header = "Journal", Icon = Application.Current?.FindResource("SemiIconBook"),
                Content = new JournalView { DataContext = DataContext }
            });
            Pages.Add(new ContentPage
            {
                Header = "Notes", Icon = Application.Current?.FindResource("SemiIconEdit2"),
                Content = new NotesView { DataContext = DataContext }
            });
            Pages.Add(new ContentPage
            {
                Header = "Settings", Icon = Application.Current?.FindResource("SemiIconSetting"),
                Content = new SettingsView { DataContext = DataContext }
            });
        }

        DrawerListBox.SelectedIndex = 0;
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