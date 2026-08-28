using System;
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

    private bool _isDataSaved = false;
    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            if (_isDataSaved)
                return;
            
            e.Cancel = true;
            
            Console.WriteLine("Closing, saving files");
            await vm.Cleanup();
            Console.WriteLine("Done!");
            
            _isDataSaved = true;
            Close();
        }
    }
}