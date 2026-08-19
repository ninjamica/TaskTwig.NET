using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.ViewModels;

public readonly struct IntervalType(Type type, string name)
{
    public Type Type { get; } = type;
    public string Name { get; } = name;
}

public partial class TaskDialogViewModel : ViewModelBase, IDialogContext
{
    public TwTask Task { get; }

    [ObservableProperty] public partial IntervalType SelectedInterval { get; set; }
    [ObservableProperty] public partial DateOnly? ReferenceDate { get; set; }
    
    private static readonly Dictionary<Type, IntervalType> _intervalTypes = new()
    {
        {typeof(NoInterval), new IntervalType(typeof(NoInterval),"No Date")},
        {typeof(SingleDateInterval), new IntervalType(typeof(SingleDateInterval),"Single Date")},
        {typeof(DailyInterval), new IntervalType(typeof(DailyInterval),"Every Day")},
        {typeof(UnitInterval), new IntervalType(typeof(UnitInterval),"Unit Interval")},
        {typeof(WeekInterval), new IntervalType(typeof(WeekInterval),"Week Interval")},
        {typeof(MonthInterval), new IntervalType(typeof(MonthInterval),"Month Interval")},
    };

    public ObservableCollection<IntervalType> IntervalTypes { get; init; } = new(_intervalTypes.Values);

    [ObservableProperty] public partial DateUnit Unit { get; set; }

    [ObservableProperty] public partial int IntervalSpacing { get; set; }

    [ObservableProperty] public partial bool IsOnM { get; set; }
    [ObservableProperty] public partial bool IsOnTu { get; set; }
    [ObservableProperty] public partial bool IsOnW { get; set; }
    [ObservableProperty] public partial bool IsOnTh { get; set; }
    [ObservableProperty] public partial bool IsOnF { get; set; }
    [ObservableProperty] public partial bool IsOnSa { get; set; }
    [ObservableProperty] public partial bool IsOnSu { get; set; }

    [ObservableProperty] public partial uint MonthDateMap { get; set; } = 0u;
    
    [ObservableProperty] public partial bool ShowOPattern { get; set; } = false;
    [ObservableProperty] public partial bool ShowEPattern { get; set; } = false;
    [ObservableProperty] public partial bool ShowDate { get; set; } = false;
    [ObservableProperty] public partial bool ShowReferenceDate { get; set; } = false;
    [ObservableProperty] public partial bool ShowUnitInterval { get; set; } = false;
    [ObservableProperty] public partial bool ShowWeekInterval { get; set; } = false;
    [ObservableProperty] public partial bool ShowMonthInterval { get; set; } = false;

    private bool _isFinishedSetup = false;

    public TaskDialogViewModel(TwTask task)
    {
        Task = task;
        SelectedInterval = _intervalTypes[task.Interval.GetType()];

        switch (task.Interval)
        {
            case NoInterval or DailyInterval:
                break;
            
            case SingleDateInterval singleDateInterval:
                ReferenceDate = singleDateInterval.Date;
                ShowOPattern = true;
                ShowDate = true;
                break;
            
            case UnitInterval unitInterval:
                ReferenceDate = unitInterval.ReferenceDate;
                Unit = unitInterval.Unit;
                IntervalSpacing = unitInterval.UnitAmount;
                ShowOPattern = true;
                ShowEPattern = true;
                ShowReferenceDate = true;
                ShowUnitInterval = true;
                break;
            
            case WeekInterval weekInterval:
                ReferenceDate = weekInterval.ReferenceDate;
                IntervalSpacing = weekInterval.WeekSpacing;
                _UpdateWeekButtons(weekInterval.DayOfWeekMap);
                ShowOPattern = true;
                ShowEPattern = true;
                ShowReferenceDate = true;
                ShowWeekInterval = true;
                break;
            
            case MonthInterval monthInterval:
                ReferenceDate = monthInterval.ReferenceDate;
                IntervalSpacing = monthInterval.MonthSpacing;
                MonthDateMap = monthInterval.DaysOfMonthMap;
                ShowOPattern = true;
                ShowEPattern = true;
                ShowReferenceDate = true;
                ShowMonthInterval = true;
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }

        _isFinishedSetup = true;
    }

    private void _UpdateWeekButtons(DayOfWeekFlag days)
    {
        IsOnM  = days.HasFlag(DayOfWeekFlag.Monday);
        IsOnTu = days.HasFlag(DayOfWeekFlag.Tuesday);
        IsOnW  = days.HasFlag(DayOfWeekFlag.Wednesday);
        IsOnTh = days.HasFlag(DayOfWeekFlag.Thursday);
        IsOnF  = days.HasFlag(DayOfWeekFlag.Friday);
        IsOnSa = days.HasFlag(DayOfWeekFlag.Saturday);
        IsOnSu = days.HasFlag(DayOfWeekFlag.Sunday);
    }

    partial void OnSelectedIntervalChanged(IntervalType value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (value.Type.BaseType == typeof(RepeatingInterval))
        {
            ShowOPattern = true;
            ShowEPattern = true;
            ShowDate = false;
            ShowReferenceDate = true;

            if (value.Type == typeof(UnitInterval))
            {
                var unitInterval = new UnitInterval { Unit = DateUnit.Day, UnitAmount = 1 };
                Task.Interval = unitInterval;
                Unit = unitInterval.Unit;
                IntervalSpacing = unitInterval.UnitAmount;
                ReferenceDate = unitInterval.ReferenceDate;
                
                ShowUnitInterval = true;
                ShowWeekInterval = false;
                ShowMonthInterval = false;
            }
            else if (value.Type == typeof(WeekInterval))
            {
                var weekInterval = new WeekInterval { DayOfWeekMap = DayOfWeekFlag.None, WeekSpacing = 1 };
                Task.Interval = weekInterval;
                IntervalSpacing = weekInterval.WeekSpacing;
                ReferenceDate = weekInterval.ReferenceDate;
                _UpdateWeekButtons(weekInterval.DayOfWeekMap);
                
                ShowUnitInterval = false;
                ShowWeekInterval = true;
                ShowMonthInterval = false;
            }
            else if (value.Type == typeof(MonthInterval))
            {
                var monthInterval = new MonthInterval();
                Task.Interval = monthInterval;
                IntervalSpacing = monthInterval.MonthSpacing;
                ReferenceDate = monthInterval.ReferenceDate;
                MonthDateMap = monthInterval.DaysOfMonthMap;
                
                ShowUnitInterval = false;
                ShowWeekInterval = false;
                ShowMonthInterval = true;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
        else
        {
            ShowEPattern = false;
            ShowReferenceDate = false;
            ShowUnitInterval = false;
            ShowWeekInterval = false;
            ShowMonthInterval = false;
            
            if (value.Type == typeof(NoInterval))
            {
                Task.Interval = new NoInterval();

                ShowOPattern = false;
                ShowDate = false;
            }
            else if (value.Type == typeof(SingleDateInterval))
            {
                var singleInterval = new SingleDateInterval { Date = Core.TaskTwig.Today };
                Task.Interval = singleInterval;
                ReferenceDate = singleInterval.Date;

                ShowOPattern = true;
                ShowDate = true;
            }
            else if (value.Type == typeof(DailyInterval))
            {
                Task.Interval = new DailyInterval();

                ShowOPattern = false;
                ShowDate = false;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
        }
    }

    partial void OnReferenceDateChanged(DateOnly? value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (value is { } date)
        {
            if (Task.Interval is SingleDateInterval singleDateInterval)
            {
                singleDateInterval.Date = date;
            }
            else if (Task.Interval is RepeatingInterval repeatingInterval)
            {
                repeatingInterval.ReferenceDate = date;
            }
        }
        
    }

    partial void OnUnitChanged(DateUnit value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (Task.Interval is UnitInterval unitInterval)
        {
            unitInterval.Unit = Unit;
        }
    }

    partial void OnIntervalSpacingChanged(int value)
    {
        if (!_isFinishedSetup)
            return;
        
        switch (Task.Interval)
        {
            case UnitInterval unitInterval:
                unitInterval.UnitAmount = value;
                break;
            case WeekInterval weekInterval:
                weekInterval.WeekSpacing = value;
                break;
            case MonthInterval monthInterval:
                monthInterval.MonthSpacing = value;
                break;
        }
    }
    
    partial void OnIsOnMChanged(bool value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (Task.Interval is WeekInterval weekInterval)
        {
            if (value)
                weekInterval.DayOfWeekMap |= DayOfWeekFlag.Monday;
            else
                weekInterval.DayOfWeekMap &= ~DayOfWeekFlag.Monday;
        }
    }

    partial void OnIsOnTuChanged(bool value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (Task.Interval is WeekInterval weekInterval)
        {
            if (value)
                weekInterval.DayOfWeekMap |= DayOfWeekFlag.Tuesday;
            else
                weekInterval.DayOfWeekMap &= ~DayOfWeekFlag.Tuesday;
        }
    }

    partial void OnIsOnWChanged(bool value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (Task.Interval is WeekInterval weekInterval)
        {
            if (value)
                weekInterval.DayOfWeekMap |= DayOfWeekFlag.Wednesday;
            else
                weekInterval.DayOfWeekMap &= ~DayOfWeekFlag.Wednesday;
        }
    }

    partial void OnIsOnThChanged(bool value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (Task.Interval is WeekInterval weekInterval)
        {
            if (value)
                weekInterval.DayOfWeekMap |= DayOfWeekFlag.Thursday;
            else
                weekInterval.DayOfWeekMap &= ~DayOfWeekFlag.Thursday;
        }
    }

    partial void OnIsOnFChanged(bool value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (Task.Interval is WeekInterval weekInterval)
        {
            if (value)
                weekInterval.DayOfWeekMap |= DayOfWeekFlag.Friday;
            else
                weekInterval.DayOfWeekMap &= ~DayOfWeekFlag.Friday;
        }
    }

    partial void OnIsOnSaChanged(bool value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (Task.Interval is WeekInterval weekInterval)
        {
            if (value)
                weekInterval.DayOfWeekMap |= DayOfWeekFlag.Saturday;
            else
                weekInterval.DayOfWeekMap &= ~DayOfWeekFlag.Saturday;
        }
    }

    partial void OnIsOnSuChanged(bool value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (Task.Interval is WeekInterval weekInterval)
        {
            if (value)
                weekInterval.DayOfWeekMap |= DayOfWeekFlag.Sunday;
            else
                weekInterval.DayOfWeekMap &= ~DayOfWeekFlag.Sunday;
        }
    }

    partial void OnMonthDateMapChanged(uint value)
    {
        if (!_isFinishedSetup)
            return;
        
        if (Task.Interval is MonthInterval monthInterval)
        {
            monthInterval.DaysOfMonthMap = value;
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