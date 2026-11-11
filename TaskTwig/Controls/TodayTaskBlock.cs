using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.Controls;

public class TodayTaskBlock : CheckBox
{
    public static readonly StyledProperty<ICommand?> EditCommandProperty = AvaloniaProperty.Register<TodayTaskBlock, ICommand?>(
        nameof(EditCommand));
    
    public ICommand? EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public static readonly StyledProperty<TwTask?> BoundTaskProperty = AvaloniaProperty.Register<TodayTaskBlock, TwTask?>(
        nameof(BoundTask));
    
    public TwTask? BoundTask
    {
        get => GetValue(BoundTaskProperty);
        set => SetValue(BoundTaskProperty, value);
    }


    protected override void OnTapped(TappedEventArgs e)
    {
        base.OnTapped(e);
        BoundTask?.SetDone(!BoundTask.IsDone);
    }

    public static readonly FuncValueConverter<bool, string> DoneMenuItemHeaderConverter =
        new(done => done ? "Mark Not Completed" : "Mark Completed");
    
    public static readonly FuncValueConverter<bool?, StreamGeometry?> DoneMenuItemIconConverter =
        new(done =>
        {
            string resourceKey = done == true ? "SemiIconClose" : "SemiIconTick";

            if (Application.Current is not null && Application.Current.TryGetResource(resourceKey, out var resource) &&
                resource is StreamGeometry icon)
            {
                return icon;
            }
            
            return null;
        });

    public static readonly FuncValueConverter<ITwigInterval, string?> TaskInfoTextConverter =
        new(interval =>
        {
            return interval switch
            {
                DailyInterval dailyInterval => "daily",
                MonthInterval monthInterval => "monthly " + monthInterval.NextOccurrence?.ToShortDateString(),
                WeekInterval weekInterval => "weekly " + weekInterval.NextOccurrence?.ToShortDateString(),
                SingleDateInterval singleDateInterval => singleDateInterval.NextOccurrence?.ToShortDateString(),
                _ => null
            };
        });

    protected override void Toggle() { }
}