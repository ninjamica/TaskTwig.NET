using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Media;
using TaskTwig.Core;
using TaskTwig.Core.TwigInterval;

namespace TaskTwig.Views;

public class TodayTaskBlock : CheckBox
{
    // public static readonly DirectProperty<TodayTaskBlock, ICommand?> EditCommandProperty = AvaloniaProperty.RegisterDirect<TodayTaskBlock, ICommand?>(
    //     nameof(EditCommand), o => o.EditCommand, (o, v) => o.EditCommand = v);
    //
    // public ICommand? EditCommand
    // {
    //     get;
    //     set
    //     {
    //         Console.WriteLine($"EditCommand: {value}");
    //         SetAndRaise(EditCommandProperty, ref field, value);
    //     }
    // }

    public static readonly StyledProperty<ICommand?> EditCommandProperty = AvaloniaProperty.Register<TodayTaskBlock, ICommand?>(
        nameof(EditCommand));
    
    public ICommand? EditCommand
    {
        get => GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    // public static readonly AttachedProperty<ICommand?> EditTaskCommandProperty =
    //     AvaloniaProperty.RegisterAttached<TodayTaskBlock, TodayTaskCatBox, ICommand?>("EditTaskCommand");
    //
    // public static void SetEditTaskCommand(TodayTaskCatBox obj, ICommand? value) => obj.SetValue(EditTaskCommandProperty, value);
    // public static ICommand? GetEditTaskCommand(TodayTaskCatBox obj) => obj.GetValue(EditTaskCommandProperty);

    // public static readonly DirectProperty<TodayTaskBlock, TwTask?> BoundTaskProperty = AvaloniaProperty.RegisterDirect<TodayTaskBlock, TwTask?>(
    //     nameof(BoundTask), o => o.BoundTask, (o, v) => o.BoundTask = v);
    //
    // public TwTask? BoundTask
    // {
    //     get;
    //     set => SetAndRaise(BoundTaskProperty, ref field, value);
    // }

    public static readonly StyledProperty<TwTask?> BoundTaskProperty = AvaloniaProperty.Register<TodayTaskBlock, TwTask?>(
        nameof(BoundTask));
    
    public TwTask? BoundTask
    {
        get => GetValue(BoundTaskProperty);
        set => SetValue(BoundTaskProperty, value);
    }


    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {   
        base.OnApplyTemplate(e);

        // if (DataContext is TwTask task)
        // {
            // PseudoClasses.Set(":done", task.IsDone);
            // Checked
            // task.PropertyChanged += OnTaskPropertyChanged;
            // IsCheckedProperty.B
        // }

        // e.NameScope.Get<CheckBox>("PART_Checkbox").Tapped += TodayTaskNameTapped;
    }

    protected override void OnDataContextBeginUpdate()
    {
        base.OnDataContextBeginUpdate();
        // if (DataContext is TwTask task)
        // {
        //     task.PropertyChanged -= OnTaskPropertyChanged;
        // }
    }

    protected override void OnDataContextEndUpdate()
    {
        base.OnDataContextEndUpdate();
        // if (DataContext is TwTask task)
        // {
        //     task.PropertyChanged += OnTaskPropertyChanged;
        //     PseudoClasses.Set(":done", task.IsDone);
        // }
    }

    private void OnTaskPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is TwTask task && e.PropertyName == nameof(task.IsDone))
        {
            PseudoClasses.Set(":done", task.IsDone);
        }
    }

    // private void TodayTaskNameTapped(object? sender, TappedEventArgs e)
    // {
    //     if (sender is Control { DataContext: TwTask task })
    //     {
    //         task.SetDone(!task.IsDone);
    //         e.Handled = true;
    //     }
    // }

    protected override void OnTapped(TappedEventArgs e)
    {
        base.OnTapped(e);
        
        BoundTask?.SetDone(!BoundTask.IsDone);
        // if (DataContext is TwTask task)
        // {
        //     task.SetDone(!task.IsDone);
        //     e.Handled = true;
        // }
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
            // if (task is null)
            //     return null;
            //
            // var infoText = task.Interval switch
            // {
            //     DailyInterval dailyInterval => "daily",
            //     MonthInterval monthInterval => "monthly ",
            //     UnitInterval unitInterval => "unit ",
            //     WeekInterval weekInterval => "weekly ",
            //     SingleDateInterval singleDateInterval => "single ",
            //     _ => ""
            // };
            //
            // if (task.Interval is RepeatingInterval { NextOccurrence: { } nextRepeat })
            // {
            //     infoText += nextRepeat.ToShortDateString();
            // }
            // else if (task.Interval is SingleDateInterval { NextOccurrence: { } nextSingle })
            // {
            //     infoText += nextSingle.ToShortDateString();
            // }
            //
            // return infoText;
            return interval switch
            {
                DailyInterval dailyInterval => "daily",
                MonthInterval monthInterval => "monthly " + monthInterval.NextOccurrence?.ToShortDateString(),
                WeekInterval weekInterval => "weekly " + weekInterval.NextOccurrence?.ToShortDateString(),
                SingleDateInterval singleDateInterval => singleDateInterval.NextOccurrence?.ToShortDateString(),
                _ => null
            };
            // return task?.Interval.NextOccurrence?.ToShortDateString();
        });
    
    // [RelayCommand]
    // private void TestCommand(TwTask task)
    // {
    //     Console.WriteLine($"TestCommand: {task.Name} -> {EditCommand}");
    //     EditCommand?.Execute(task);
    //     Console.WriteLine("Finished TestCommand");
    // }

    // protected override void OnClick() { }

    protected override void Toggle() { }
}

public class ReadOnlyCheckBox : CheckBox
{
    protected override void OnClick() { }
}