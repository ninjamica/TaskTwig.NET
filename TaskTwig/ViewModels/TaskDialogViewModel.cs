using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.ViewModels;

public readonly record struct IntervalType(Type Type, string Name);

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

public class EmojiTextBox : TextBox
{
    // Source - https://stackoverflow.com/a/48148218
    // Posted by Wiktor Stribiżew, modified by community. See post 'Timeline' for change history
    // Retrieved 2026-08-24, License - CC BY-SA 4.0
    private const string EmojiPattern = @"[#*0-9]\uFE0F?\u20E3|\u00A9\uFE0F?|[\u00AE\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9\u21AA]\uFE0F?|[\u231A\u231B]|[\u2328\u23CF]\uFE0F?|[\u23E9-\u23EC]|[\u23ED-\u23EF]\uFE0F?|\u23F0|[\u23F1\u23F2]\uFE0F?|\u23F3|[\u23F8-\u23FA\u24C2\u25AA\u25AB\u25B6\u25C0\u25FB\u25FC]\uFE0F?|[\u25FD\u25FE]|[\u2600-\u2604\u260E\u2611]\uFE0F?|[\u2614\u2615]|\u2618\uFE0F?|\u261D(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|[\u2620\u2622\u2623\u2626\u262A\u262E\u262F\u2638-\u263A\u2640\u2642]\uFE0F?|[\u2648-\u2653]|[\u265F\u2660\u2663\u2665\u2666\u2668\u267B\u267E]\uFE0F?|\u267F|\u2692\uFE0F?|\u2693|[\u2694-\u2697\u2699\u269B\u269C\u26A0]\uFE0F?|\u26A1|\u26A7\uFE0F?|[\u26AA\u26AB]|[\u26B0\u26B1]\uFE0F?|[\u26BD\u26BE\u26C4\u26C5]|\u26C8\uFE0F?|\u26CE|[\u26CF\u26D1]\uFE0F?|\u26D3(?:\u200D\uD83D\uDCA5|\uFE0F(?:\u200D\uD83D\uDCA5)?)?|\u26D4|\u26E9\uFE0F?|\u26EA|[\u26F0\u26F1]\uFE0F?|[\u26F2\u26F3]|\u26F4\uFE0F?|\u26F5|[\u26F7\u26F8]\uFE0F?|\u26F9(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?|\uFE0F(?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\u26FA\u26FD]|\u2702\uFE0F?|\u2705|[\u2708\u2709]\uFE0F?|[\u270A\u270B](?:\uD83C[\uDFFB-\uDFFF])?|[\u270C\u270D](?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|\u270F\uFE0F?|[\u2712\u2714\u2716\u271D\u2721]\uFE0F?|\u2728|[\u2733\u2734\u2744\u2747]\uFE0F?|[\u274C\u274E\u2753-\u2755\u2757]|\u2763\uFE0F?|\u2764(?:\u200D(?:\uD83D\uDD25|\uD83E\uDE79)|\uFE0F(?:\u200D(?:\uD83D\uDD25|\uD83E\uDE79))?)?|[\u2795-\u2797]|\u27A1\uFE0F?|[\u27B0\u27BF]|[\u2934\u2935\u2B05-\u2B07]\uFE0F?|[\u2B1B\u2B1C\u2B50\u2B55]|[\u3030\u303D\u3297\u3299]\uFE0F?|\uD83C(?:[\uDC04\uDCCF]|[\uDD70\uDD71\uDD7E\uDD7F]\uFE0F?|[\uDD8E\uDD91-\uDD9A]|\uDDE6\uD83C[\uDDE8-\uDDEC\uDDEE\uDDF1\uDDF2\uDDF4\uDDF6-\uDDFA\uDDFC\uDDFD\uDDFF]|\uDDE7\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEF\uDDF1-\uDDF4\uDDF6-\uDDF9\uDDFB\uDDFC\uDDFE\uDDFF]|\uDDE8\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDEE\uDDF0-\uDDF7\uDDFA-\uDDFF]|\uDDE9\uD83C[\uDDEA\uDDEC\uDDEF\uDDF0\uDDF2\uDDF4\uDDFF]|\uDDEA\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDED\uDDF7-\uDDFA]|\uDDEB\uD83C[\uDDEE-\uDDF0\uDDF2\uDDF4\uDDF7]|\uDDEC\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEE\uDDF1-\uDDF3\uDDF5-\uDDFA\uDDFC\uDDFE]|\uDDED\uD83C[\uDDF0\uDDF2\uDDF3\uDDF7\uDDF9\uDDFA]|\uDDEE\uD83C[\uDDE8-\uDDEA\uDDF1-\uDDF4\uDDF6-\uDDF9]|\uDDEF\uD83C[\uDDEA\uDDF2\uDDF4\uDDF5]|\uDDF0\uD83C[\uDDEA\uDDEC-\uDDEE\uDDF2\uDDF3\uDDF5\uDDF7\uDDFC\uDDFE\uDDFF]|\uDDF1\uD83C[\uDDE6-\uDDE8\uDDEE\uDDF0\uDDF7-\uDDFB\uDDFE]|\uDDF2\uD83C[\uDDE6\uDDE8-\uDDED\uDDF0-\uDDFF]|\uDDF3\uD83C[\uDDE6\uDDE8\uDDEA-\uDDEC\uDDEE\uDDF1\uDDF4\uDDF5\uDDF7\uDDFA\uDDFF]|\uDDF4\uD83C\uDDF2|\uDDF5\uD83C[\uDDE6\uDDEA-\uDDED\uDDF0-\uDDF3\uDDF7-\uDDF9\uDDFC\uDDFE]|\uDDF6\uD83C\uDDE6|\uDDF7\uD83C[\uDDEA\uDDF4\uDDF8\uDDFA\uDDFC]|\uDDF8\uD83C[\uDDE6-\uDDEA\uDDEC-\uDDF4\uDDF7-\uDDF9\uDDFB\uDDFD-\uDDFF]|\uDDF9\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDED\uDDEF-\uDDF4\uDDF7\uDDF9\uDDFB\uDDFC\uDDFF]|\uDDFA\uD83C[\uDDE6\uDDEC\uDDF2\uDDF3\uDDF8\uDDFE\uDDFF]|\uDDFB\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDEE\uDDF3\uDDFA]|\uDDFC\uD83C[\uDDEB\uDDF8]|\uDDFD\uD83C\uDDF0|\uDDFE\uD83C[\uDDEA\uDDF9]|\uDDFF\uD83C[\uDDE6\uDDF2\uDDFC]|\uDE01|\uDE02\uFE0F?|[\uDE1A\uDE2F\uDE32-\uDE36]|\uDE37\uFE0F?|[\uDE38-\uDE3A\uDE50\uDE51\uDF00-\uDF20]|[\uDF21\uDF24-\uDF2C]\uFE0F?|[\uDF2D-\uDF35]|\uDF36\uFE0F?|[\uDF37-\uDF43]|\uDF44(?:\u200D\uD83D\uDFEB)?|[\uDF45-\uDF4A]|\uDF4B(?:\u200D\uD83D\uDFE9)?|[\uDF4C-\uDF7C]|\uDF7D\uFE0F?|[\uDF7E-\uDF84]|\uDF85(?:\uD83C[\uDFFB-\uDFFF])?|[\uDF86-\uDF93]|[\uDF96\uDF97\uDF99-\uDF9B\uDF9E\uDF9F]\uFE0F?|[\uDFA0-\uDFC1]|\uDFC2(?:\uD83C[\uDFFB-\uDFFF])?|\uDFC3(?:\u200D(?:[\u2640\u2642](?:\u200D\u27A1\uFE0F?|\uFE0F(?:\u200D\u27A1\uFE0F?)?)?|\u27A1\uFE0F?)|\uD83C[\uDFFB-\uDFFF](?:\u200D(?:[\u2640\u2642](?:\u200D\u27A1\uFE0F?|\uFE0F(?:\u200D\u27A1\uFE0F?)?)?|\u27A1\uFE0F?))?)?|\uDFC4(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDFC5\uDFC6]|\uDFC7(?:\uD83C[\uDFFB-\uDFFF])?|[\uDFC8\uDFC9]|\uDFCA(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDFCB\uDFCC](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?|\uFE0F(?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDFCD\uDFCE]\uFE0F?|[\uDFCF-\uDFD3]|[\uDFD4-\uDFDF]\uFE0F?|[\uDFE0-\uDFF0]|\uDFF3(?:\u200D(?:\u26A7\uFE0F?|\uD83C\uDF08)|\uFE0F(?:\u200D(?:\u26A7\uFE0F?|\uD83C\uDF08))?)?|\uDFF4(?:\u200D\u2620\uFE0F?|\uDB40\uDC67\uDB40\uDC62\uDB40(?:\uDC65\uDB40\uDC6E\uDB40\uDC67|\uDC73\uDB40\uDC63\uDB40\uDC74|\uDC77\uDB40\uDC6C\uDB40\uDC73)\uDB40\uDC7F)?|[\uDFF5\uDFF7]\uFE0F?|[\uDFF8-\uDFFF])|\uD83D(?:[\uDC00-\uDC07]|\uDC08(?:\u200D\u2B1B)?|[\uDC09-\uDC14]|\uDC15(?:\u200D\uD83E\uDDBA)?|[\uDC16-\uDC25]|\uDC26(?:\u200D(?:\u2B1B|\uD83D\uDD25))?|[\uDC27-\uDC3A]|\uDC3B(?:\u200D\u2744\uFE0F?)?|[\uDC3C-\uDC3E]|\uDC3F\uFE0F?|\uDC40|\uDC41(?:\u200D\uD83D\uDDE8\uFE0F?|\uFE0F(?:\u200D\uD83D\uDDE8\uFE0F?)?)?|[\uDC42\uDC43](?:\uD83C[\uDFFB-\uDFFF])?|[\uDC44\uDC45]|[\uDC46-\uDC50](?:\uD83C[\uDFFB-\uDFFF])?|[\uDC51-\uDC65]|[\uDC66\uDC67](?:\uD83C[\uDFFB-\uDFFF])?|\uDC68(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?|[\uDC68\uDC69]\u200D\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|\uD83E(?:\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?))|\uD83C(?:\uDFFB(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFC-\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFC(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB\uDFFD-\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFD(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFE(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB-\uDFFD\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFF(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB-\uDFFE]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?))?|\uDC69(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:\uDC8B\u200D\uD83D)?[\uDC68\uDC69]|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?|\uDC69\u200D\uD83D(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|\uD83E(?:\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?))|\uD83C(?:\uDFFB(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFC-\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFC(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB\uDFFD-\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFD(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFE(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFD\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFF(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D\uD83D(?:[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|\uDC8B\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF])|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFE]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?))?|\uDC6A|[\uDC6B-\uDC6D](?:\uD83C[\uDFFB-\uDFFF])?|\uDC6E(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC6F(?:\u200D[\u2640\u2642]\uFE0F?)?|[\uDC70\uDC71](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC72(?:\uD83C[\uDFFB-\uDFFF])?|\uDC73(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDC74-\uDC76](?:\uD83C[\uDFFB-\uDFFF])?|\uDC77(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC78(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC79-\uDC7B]|\uDC7C(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC7D-\uDC80]|[\uDC81\uDC82](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDC83(?:\uD83C[\uDFFB-\uDFFF])?|\uDC84|\uDC85(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC86\uDC87](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDC88-\uDC8E]|\uDC8F(?:\uD83C[\uDFFB-\uDFFF])?|\uDC90|\uDC91(?:\uD83C[\uDFFB-\uDFFF])?|[\uDC92-\uDCA9]|\uDCAA(?:\uD83C[\uDFFB-\uDFFF])?|[\uDCAB-\uDCFC]|\uDCFD\uFE0F?|[\uDCFF-\uDD3D]|[\uDD49\uDD4A]\uFE0F?|[\uDD4B-\uDD4E\uDD50-\uDD67]|[\uDD6F\uDD70\uDD73]\uFE0F?|\uDD74(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|\uDD75(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?|\uFE0F(?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDD76-\uDD79]\uFE0F?|\uDD7A(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD87\uDD8A-\uDD8D]\uFE0F?|\uDD90(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)?|[\uDD95\uDD96](?:\uD83C[\uDFFB-\uDFFF])?|\uDDA4|[\uDDA5\uDDA8\uDDB1\uDDB2\uDDBC\uDDC2-\uDDC4\uDDD1-\uDDD3\uDDDC-\uDDDE\uDDE1\uDDE3\uDDE8\uDDEF\uDDF3\uDDFA]\uFE0F?|[\uDDFB-\uDE2D]|\uDE2E(?:\u200D\uD83D\uDCA8)?|[\uDE2F-\uDE34]|\uDE35(?:\u200D\uD83D\uDCAB)?|\uDE36(?:\u200D\uD83C\uDF2B\uFE0F?)?|[\uDE37-\uDE41]|\uDE42(?:\u200D[\u2194\u2195]\uFE0F?)?|[\uDE43\uDE44]|[\uDE45-\uDE47](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDE48-\uDE4A]|\uDE4B(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDE4C(?:\uD83C[\uDFFB-\uDFFF])?|[\uDE4D\uDE4E](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDE4F(?:\uD83C[\uDFFB-\uDFFF])?|[\uDE80-\uDEA2]|\uDEA3(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDEA4-\uDEB3]|[\uDEB4\uDEB5](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDEB6(?:\u200D(?:[\u2640\u2642](?:\u200D\u27A1\uFE0F?|\uFE0F(?:\u200D\u27A1\uFE0F?)?)?|\u27A1\uFE0F?)|\uD83C[\uDFFB-\uDFFF](?:\u200D(?:[\u2640\u2642](?:\u200D\u27A1\uFE0F?|\uFE0F(?:\u200D\u27A1\uFE0F?)?)?|\u27A1\uFE0F?))?)?|[\uDEB7-\uDEBF]|\uDEC0(?:\uD83C[\uDFFB-\uDFFF])?|[\uDEC1-\uDEC5]|\uDECB\uFE0F?|\uDECC(?:\uD83C[\uDFFB-\uDFFF])?|[\uDECD-\uDECF]\uFE0F?|[\uDED0-\uDED2\uDED5-\uDED7\uDEDC-\uDEDF]|[\uDEE0-\uDEE5\uDEE9]\uFE0F?|[\uDEEB\uDEEC]|[\uDEF0\uDEF3]\uFE0F?|[\uDEF4-\uDEFC\uDFE0-\uDFEB\uDFF0])|\uD83E(?:\uDD0C(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD0D\uDD0E]|\uDD0F(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD10-\uDD17]|[\uDD18-\uDD1F](?:\uD83C[\uDFFB-\uDFFF])?|[\uDD20-\uDD25]|\uDD26(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDD27-\uDD2F]|[\uDD30-\uDD34](?:\uD83C[\uDFFB-\uDFFF])?|\uDD35(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDD36(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD37-\uDD39](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDD3A|\uDD3C(?:\u200D[\u2640\u2642]\uFE0F?)?|[\uDD3D\uDD3E](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDD3F-\uDD45\uDD47-\uDD76]|\uDD77(?:\uD83C[\uDFFB-\uDFFF])?|[\uDD78-\uDDB4]|[\uDDB5\uDDB6](?:\uD83C[\uDFFB-\uDFFF])?|\uDDB7|[\uDDB8\uDDB9](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDBA|\uDDBB(?:\uD83C[\uDFFB-\uDFFF])?|[\uDDBC-\uDDCC]|\uDDCD(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDCE(?:\u200D(?:[\u2640\u2642](?:\u200D\u27A1\uFE0F?|\uFE0F(?:\u200D\u27A1\uFE0F?)?)?|\u27A1\uFE0F?)|\uD83C[\uDFFB-\uDFFF](?:\u200D(?:[\u2640\u2642](?:\u200D\u27A1\uFE0F?|\uFE0F(?:\u200D\u27A1\uFE0F?)?)?|\u27A1\uFE0F?))?)?|\uDDCF(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDD0|\uDDD1(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?|(?:\uDDD1\u200D\uD83E)?\uDDD2(?:\u200D\uD83E\uDDD2)?))|\uD83C(?:\uDFFB(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFC-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFC(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB\uDFFD-\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFD(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFE(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB-\uDFFD\uDFFF]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?|\uDFFF(?:\u200D(?:[\u2695\u2696\u2708]\uFE0F?|\u2764\uFE0F?\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB-\uDFFE]|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|\uDDAF(?:\u200D\u27A1\uFE0F?)?|[\uDDB0-\uDDB3]|[\uDDBC\uDDBD](?:\u200D\u27A1\uFE0F?)?)))?))?|[\uDDD2\uDDD3](?:\uD83C[\uDFFB-\uDFFF])?|\uDDD4(?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|\uDDD5(?:\uD83C[\uDFFB-\uDFFF])?|[\uDDD6-\uDDDD](?:\u200D[\u2640\u2642]\uFE0F?|\uD83C[\uDFFB-\uDFFF](?:\u200D[\u2640\u2642]\uFE0F?)?)?|[\uDDDE\uDDDF](?:\u200D[\u2640\u2642]\uFE0F?)?|[\uDDE0-\uDDFF\uDE70-\uDE7C\uDE80-\uDE89\uDE8F-\uDEC2]|[\uDEC3-\uDEC5](?:\uD83C[\uDFFB-\uDFFF])?|[\uDEC6\uDECE-\uDEDC\uDEDF-\uDEE9]|\uDEF0(?:\uD83C[\uDFFB-\uDFFF])?|\uDEF1(?:\uD83C(?:\uDFFB(?:\u200D\uD83E\uDEF2\uD83C[\uDFFC-\uDFFF])?|\uDFFC(?:\u200D\uD83E\uDEF2\uD83C[\uDFFB\uDFFD-\uDFFF])?|\uDFFD(?:\u200D\uD83E\uDEF2\uD83C[\uDFFB\uDFFC\uDFFE\uDFFF])?|\uDFFE(?:\u200D\uD83E\uDEF2\uD83C[\uDFFB-\uDFFD\uDFFF])?|\uDFFF(?:\u200D\uD83E\uDEF2\uD83C[\uDFFB-\uDFFE])?))?|[\uDEF2-\uDEF8](?:\uD83C[\uDFFB-\uDFFF])?)";

    protected override Type StyleKeyOverride => typeof(TextBox);

    protected override async void OnKeyDown(KeyEventArgs e)
    {
        if (e is { Key: Key.V, KeyModifiers: KeyModifiers.Control })
        {
            var clipboard = await TopLevel.GetTopLevel(visual: this)?.Clipboard?.TryGetTextAsync();
            Console.WriteLine($"IconBoxPastingFromClipboard new text: {clipboard ?? "null"}");

            if (clipboard != null && Regex.IsMatch(clipboard, EmojiPattern))
            {
                Clear();
            }
            else
            {
                e.Handled = true;
                return;
            }
        }

        base.OnKeyDown(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        Console.WriteLine($"IconBoxTextInput new text: {e.Text}");

        
        e.Handled = true;
        if (e.Text is { } text && Regex.IsMatch(text, EmojiPattern))
        {
            Text = text;
            CaretIndex = text.Length;
        }
    }
}