using Avalonia;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.Views;

public class WeekDaySelect : TemplatedControl
{
    public static readonly StyledProperty<DayOfWeekFlag> WeekDayMapProperty = AvaloniaProperty.Register<WeekDaySelect, DayOfWeekFlag>(
        nameof(WeekDayMap), defaultBindingMode: BindingMode.TwoWay);

    public DayOfWeekFlag WeekDayMap
    {
        get => GetValue(WeekDayMapProperty);
        set => SetValue(WeekDayMapProperty, value);
    }
}


public class WeekDayButton : ToggleButton
{
    public static readonly StyledProperty<DayOfWeekFlag> DayProperty = AvaloniaProperty.Register<WeekDayButton, DayOfWeekFlag>(
        nameof(Day));

    public static readonly StyledProperty<DayOfWeekFlag> WeekDayMapProperty = AvaloniaProperty.Register<WeekDayButton, DayOfWeekFlag>(
        nameof(WeekDayMap), defaultBindingMode: BindingMode.TwoWay);

    public DayOfWeekFlag Day
    {
        get => GetValue(DayProperty);
        set => SetValue(DayProperty, value);
    }

    public DayOfWeekFlag WeekDayMap
    {
        get => GetValue(WeekDayMapProperty);
        set => SetValue(WeekDayMapProperty, value);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        
        IsChecked = WeekDayMap.HasFlag(Day);
    }

    protected override void Toggle()
    {
        WeekDayMap ^= Day;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == WeekDayMapProperty)
        {
            IsChecked = change.GetNewValue<DayOfWeekFlag>().HasFlag(Day);
        }
    }
}