using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using TaskTwig.Core;
using TaskTwig.ViewModels;
using TaskTwig.Views;

namespace TaskTwig;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory =
                () => new MainView { DataContext = new MainViewModel() };
            
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        if (this.TryGetFeature<IActivatableLifetime>() is { } activatableLifetime)
        {
            activatableLifetime.Activated += (sender, args) =>
            {
                Console.WriteLine("Lifetime activated!");
                
                TwigTime.RefreshToday();
                HashableObject.ResetFetchTimer(true);
            };

            activatableLifetime.Deactivated += (sender, args) =>
            {
                HashableObject.StopFetchTimer();
                TwigTime.StopTodayTimer();

                Console.WriteLine("Lifetime deactivated!");
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}