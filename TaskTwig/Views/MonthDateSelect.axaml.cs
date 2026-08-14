using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;
using ObservableCollections;
using TaskTwig.ViewModels;

namespace TaskTwig.Views;

public partial class MonthDateSelect : UserControl
{
    private readonly ObservableCollection<int> _dates =
    [
        1, 2, 3, 4, 5, 6, 7,
        8, 9, 10, 11, 12, 13, 14,
        15, 16, 17, 18, 19, 20, 21,
        22, 23, 24, 25, 26, 27, 28,
        29, 30, 31
    ];
    
    public static readonly StyledProperty<uint> DateMapProperty =
        AvaloniaProperty.Register<MonthDateSelect, uint>(nameof(DateMap), defaultBindingMode: BindingMode.TwoWay, defaultValue: 0u);
    
    public uint DateMap
    {
        get => GetValue(DateMapProperty);
        set
        {
            SetValue(DateMapProperty, value);
            SetDateMap(value);
        }
    }
    
    public MonthDateSelect()
    {
        InitializeComponent();
        DatesControl.ItemsSource = _dates;
    }

    private void SetDateMap(uint map)
    {
        Console.WriteLine($"DateMap: {map}");
        foreach (var control in DatesControl.ItemsPanelRoot.Children)
        {
            var toggleButton = (ToggleButton)control;
            int date = (int)toggleButton.Content;
            toggleButton.IsChecked = (map & ~(1u << (date - 1))) != 0;
            
            Console.WriteLine($"{date}={toggleButton.IsChecked}");
        }
    }

    private void DateCheckChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is ToggleButton { Content: int date } dateButton)
        {
            if (dateButton.IsChecked == true)
                SetValue(DateMapProperty, DateMap | (1u << (date - 1)));
            else
                SetValue(DateMapProperty, DateMap & ~(1u << (date - 1)));
            
            Console.WriteLine($"MonthDateSelect: {date}={dateButton.IsChecked}: {DateMap}");
        }
    }   
}