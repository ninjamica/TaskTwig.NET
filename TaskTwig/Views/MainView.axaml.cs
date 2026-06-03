using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Diagnostics;

namespace TaskTwig.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
    }

    // private void Button_OnClick(object? sender, RoutedEventArgs e)
    // {
    //     if (double.TryParse(Celsius.Text, out double C))
    //     {
    //         var F = C * (9d / 5d) + 32;
    //         Fahrenheit.Text = F.ToString("0.0");
    //     }
    //     else
    //     {
    //         Celsius.Text = "0";
    //         Fahrenheit.Text = "0";
    //     }
    // }
}