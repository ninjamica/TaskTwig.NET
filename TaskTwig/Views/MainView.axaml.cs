using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
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
}