using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace TaskTwig.Views;

public partial class ExpanderButton : UserControl
{
    public static readonly StyledProperty<bool> IsExpandedProperty =
        AvaloniaProperty.Register<ExpanderButton, bool>(nameof(IsExpanded), defaultBindingMode: BindingMode.TwoWay, defaultValue: true);
    
    public bool IsExpanded
    {
        get => GetValue(IsExpandedProperty);
        set => SetValue(IsExpandedProperty, value);
    }

    public ExpanderButton() => InitializeComponent();

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        SetCurrentValue(IsExpandedProperty, !IsExpanded);
    }
}