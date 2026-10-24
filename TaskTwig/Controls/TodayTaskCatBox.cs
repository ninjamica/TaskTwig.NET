using System;
using System.Collections.Generic;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using CommunityToolkit.Mvvm.Input;
using TaskTwig.Core;

namespace TaskTwig.Controls;

[TemplatePart(Name = "PART_HeaderGrid", Type = typeof(Grid), IsRequired = true)]
public partial class TodayTaskCatBox : TemplatedControl
{
    public static readonly StyledProperty<string?> HeaderTextProperty = AvaloniaProperty.Register<TodayTaskCatBox, string?>(
        nameof(HeaderText));

    public static readonly StyledProperty<bool> IsExpandedProperty = AvaloniaProperty.Register<TodayTaskCatBox, bool>(
        nameof(IsExpanded));
    
    public static readonly StyledProperty<ContextMenu?> HeaderContextMenuProperty = AvaloniaProperty.Register<TodayTaskCatBox, ContextMenu?>(
        nameof(HeaderContextMenu));

    public static readonly DirectProperty<TodayTaskCatBox, IEnumerable<TwTask>?> TasksSourceProperty = AvaloniaProperty.RegisterDirect<TodayTaskCatBox, IEnumerable<TwTask>?>(
        nameof(TasksSource), o => o.TasksSource, (o, v) => o.TasksSource = v);

    public static readonly StyledProperty<ICommand?> EditTaskCommandProperty = AvaloniaProperty.Register<TodayTaskCatBox, ICommand?>(
        nameof(EditTaskCommand));
    
    public ICommand? EditTaskCommand
    {
        get => GetValue(EditTaskCommandProperty);
        set => SetValue(EditTaskCommandProperty, value);
    }

    public IEnumerable<TwTask>? TasksSource
    {
        get;
        set => SetAndRaise(TasksSourceProperty, ref field, value);
    }
    
    public string? HeaderText
    {
        get => GetValue(HeaderTextProperty);
        set => SetValue(HeaderTextProperty, value);
    }

    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }
    
    public ContextMenu? HeaderContextMenu
    {
        get => GetValue(HeaderContextMenuProperty);
        set => SetValue(HeaderContextMenuProperty, value);
    }
    
    
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        e.NameScope.Get<Grid>("PART_HeaderGrid").Tapped += HeaderTapped;
    }

    private void HeaderTapped(object? sender, TappedEventArgs e)
    {
        IsExpanded = !IsExpanded;
        e.Handled = true;
    }

    [RelayCommand]
    private void TestCatBox(TwTask task)
    {
        Console.WriteLine($"TestCatBox: {task.Name} -> {EditTaskCommand}");
        EditTaskCommand?.Execute(task);
        Console.WriteLine("Finished TestCatBox");
    }
}