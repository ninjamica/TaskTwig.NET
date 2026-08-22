using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ObservableCollections;
using TaskTwig.ViewModels;
using Ursa.Controls;

namespace TaskTwig.Views;

public partial class MonthDateSelect : UserControl
{
    public static readonly StyledProperty<uint> DateMapProperty =
        AvaloniaProperty.Register<MonthDateSelect, uint>(nameof(DateMap), defaultBindingMode: BindingMode.TwoWay, inherits:true, defaultValue: 0u);
    
    public uint DateMap
    {
        get => GetValue(DateMapProperty);
        set => SetValue(DateMapProperty, value);
    }
    
    public MonthDateSelect() => InitializeComponent();
}


[PseudoClasses(":selected", ":start-date", ":end-date", "mid-date")]
public class MonthDateCell : Button
{
    public static readonly DirectProperty<MonthDateCell, int> DateProperty =
        AvaloniaProperty.RegisterDirect<MonthDateCell, int>(
            nameof(Date),
            cell => cell.Date,
            (cell, date) => cell.Date = date);

    private int _date;
    public int Date
    {
        get => _date;
        set
        {
            SetAndRaise(DateProperty, ref _date, value);
            Content = value;
        }
    }

    public static readonly StyledProperty<uint> DateMapProperty = MonthDateSelect.DateMapProperty.AddOwner<MonthDateCell>();
    
    public uint DateMap
    {
        get => GetValue(DateMapProperty);
        set => SetValue(DateMapProperty, value);
    }
    
    protected override Type StyleKeyOverride => typeof(DatePickerCalendarDayButton);

    public MonthDateCell()
    {
        // PseudoClasses.Add(":in-range");
    }

    protected override void OnClick()
    {
        base.OnClick();
        
        if (IsSelected(_date))
            DateMap &= ~(1u << (_date - 1));
        else
            DateMap |= 1u << (_date - 1);
    }
    
    private bool IsSelected(int date) => (DateMap & (1u << (date - 1))) != 0;
    
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == DateMapProperty)
        {
            var selected = IsSelected(_date);
            PseudoClasses.Set(":selected", selected);
            
            if (selected)
            {
                var prevSelected = _date > 1 && IsSelected(_date - 1);
                var nextSelected = _date < 31 && IsSelected(_date + 1);

                PseudoClasses.Set(":start-date", !prevSelected && nextSelected);
                PseudoClasses.Set(":end-date", prevSelected && !nextSelected);
                PseudoClasses.Set(":in-range", nextSelected && prevSelected);
            }
            else
            {
                PseudoClasses.Remove(":start-date");
                PseudoClasses.Remove(":end-date");
                PseudoClasses.Remove(":in-range");
            }
        }
    }
}