using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;

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

    protected override void OnTapped(TappedEventArgs e)
    {
        base.OnTapped(e);
        Toggle();
    }
    
    public void Toggle()
    {
        SetCurrentValue(IsExpandedProperty, !IsExpanded);
    }
}