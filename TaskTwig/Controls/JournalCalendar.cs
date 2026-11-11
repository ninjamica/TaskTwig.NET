using System;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using CommunityToolkit.Mvvm.Input;
using Calendar = Avalonia.Controls.Calendar;

namespace TaskTwig.Controls;

public partial class JournalCalendar : Calendar
{
    public static readonly StyledProperty<ICommand?> DayCommandProperty = AvaloniaProperty.Register<JournalCalendar, ICommand?>(
        nameof(DayCommand));

    public ICommand? DayCommand
    {
        get => GetValue(DayCommandProperty);
        set => SetValue(DayCommandProperty, value);
    }

    [RelayCommand]
    private void DayButtonPressed(CalendarDayButton? button)
    {
        if (button is null)
            return;
        
        var date = GetDateFromButton(button);
        DayCommand?.Execute(date);
    }

    private DateTime GetDateFromButton(CalendarDayButton button)
    {
        var hasDay = int.TryParse(button.Content as string, out var day);
        if (!hasDay)
            throw new InvalidCastException();

        var firstDayIndex = (int)DisplayDate.DayOfWeek + 7;
        var index = button.GetLogicalParent<Grid>()?.Children.IndexOf(button);
        
        if (index is null)
            throw new InvalidCastException();
    
        var date = new DateTime(DisplayDate.Year, DisplayDate.Month, day);
        if (index < firstDayIndex)
            date = date.AddMonths(-1);
        else if (index > firstDayIndex + DateTime.DaysInMonth(DisplayDate.Year, DisplayDate.Month))
            date = date.AddMonths(1);
    
        return date;
    }
}