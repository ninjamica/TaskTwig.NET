using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;
using TaskTwig.ViewModels;

namespace TaskTwig.Dialog.ViewModels;

public partial class TaskDialogViewModel : ViewModelBase, IDialogContext
{
    public OccurrencePattern[] DuePatternValues { get; } = Enum.GetValues<OccurrencePattern>();
    public AutoExtendPattern[] ExtendValues { get; } = Enum.GetValues<AutoExtendPattern>();
    
    [ObservableProperty]
    public partial TwTask? Task { get; set; }

    [ObservableProperty] public partial Type? SelectedInterval { get; set; }
    

    public static List<Type> IntervalTypes { get; } =
    [
        typeof(NoInterval),
        typeof(DailyInterval),
        typeof(SingleDateInterval),
        typeof(UnitInterval),
        typeof(WeekInterval),
        typeof(MonthInterval),
    ];

    partial void OnTaskChanged(TwTask? value)
    {
        SelectedInterval = value?.Interval.GetType();
    }

    partial void OnSelectedIntervalChanged(Type? value)
    {
        if (Task?.Interval.GetType() != value)
        {
            Task?.Interval = value switch
            {
                _ when value == typeof(NoInterval) => new NoInterval(),
                _ when value == typeof(SingleDateInterval) => new SingleDateInterval { Date = TwigTime.Today },
                _ when value == typeof(DailyInterval) => new DailyInterval(),
                _ when value == typeof(UnitInterval) => new UnitInterval(),
                _ when value == typeof(WeekInterval) => new WeekInterval(),
                _ when value == typeof(MonthInterval) => new MonthInterval(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    [RelayCommand]
    public void Close()
    {
        RequestClose?.Invoke(this, false);
    }

    [RelayCommand]
    public void Delete()
    {
        RequestClose?.Invoke(this, true);
    }

    public event EventHandler<object?>? RequestClose;
}