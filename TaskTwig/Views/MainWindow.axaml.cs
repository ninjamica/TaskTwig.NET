using System.ComponentModel;
using Avalonia.Controls;
using TaskTwig.ViewModels;

namespace TaskTwig.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.Closing += OnWindowClosing;
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Cleanup();
        }
    }
}