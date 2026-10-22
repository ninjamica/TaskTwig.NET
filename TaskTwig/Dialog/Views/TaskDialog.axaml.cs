using System;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.Dialog.Views;

public partial class TaskDialog : UserControl
{
    public TaskDialog()
    {
        InitializeComponent();
    }

    public static readonly FuncValueConverter<int, string> WeekSpinnerTextConverter =
        new(counter => counter > 1 ? "Weeks" : "Week");
    
    public static readonly FuncValueConverter<int, string> MonthSpinnerTextConverter =
        new(counter => counter > 1 ? "Months" : "Month");

    public static readonly FuncValueConverter<ITwigInterval?, bool> ShowOPatternConverter =
        new(interval => interval is SingleDateInterval or RepeatingInterval);

    public static readonly FuncValueConverter<ITwigInterval?, bool> IsSingleIntervalConverter =
        new(interval => interval is SingleDateInterval);

    public static readonly FuncValueConverter<ITwigInterval?, bool> IsRepeatingIntervalConverter =
        new(interval => interval is RepeatingInterval);

    public static readonly FuncValueConverter<ITwigInterval?, bool> IsUnitIntervalConverter =
        new(interval => interval is UnitInterval);

    public static readonly FuncValueConverter<ITwigInterval?, bool> IsWeekOrMonthIntervalConverter =
        new(interval => interval is WeekInterval or MonthInterval);
    
    public static readonly FuncValueConverter<Type, string?> IntervalTypeNameConverter =
        new(intervalType => intervalType switch
        {
            _ when intervalType == typeof(NoInterval) => "No Date",
            _ when intervalType == typeof(DailyInterval) => "Daily",
            _ when intervalType == typeof(SingleDateInterval) => "Single Date",
            _ when intervalType == typeof(UnitInterval) => "Repeating Interval",
            _ when intervalType == typeof(WeekInterval) => "Week Based",
            _ when intervalType == typeof(MonthInterval) => "Month Based",
            _ => string.Empty
        });
    
    public static readonly FuncValueConverter<AutoExtendPattern, string?> ExtendPatternNameConverter =
        new(extendPattern => extendPattern switch
        {
            AutoExtendPattern.NoExtend => "Never skip missed",
            AutoExtendPattern.OnCompletion => "Skip missed after completing",
            AutoExtendPattern.FromCompletion => "Restart from completion date",
            AutoExtendPattern.Auto => "Auto skip missed",
            _ => ""
        });
    
    public static readonly FuncValueConverter<OccurrencePattern, string?> DuePatternNameConverter =
        new(extendPattern => extendPattern switch
        {
            OccurrencePattern.DueBy => "Due by",
            OccurrencePattern.OccurOn => "Occur on",
            OccurrencePattern.StartOn => "Start on",
            _ => ""
        });
}