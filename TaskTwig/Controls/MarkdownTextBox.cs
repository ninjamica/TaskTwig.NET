using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Markdown.Avalonia;
using Ursa.Controls;

namespace TaskTwig.Controls;

[TemplatePart(Name = "PART_Markdown", Type = typeof(MarkdownScrollViewer), IsRequired = true)]
[TemplatePart(Name = "PART_TextBox", Type = typeof(TextBox), IsRequired = true)]
public class MarkdownTextBox : TemplatedControl
{
    public static readonly StyledProperty<string?> MarkdownProperty =
        AvaloniaProperty.Register<MarkdownTextBox, string?>(nameof(Markdown), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string?> PlaceholderTextProperty = AvaloniaProperty.Register<MarkdownTextBox, string?>(
        nameof(PlaceholderText));

    public static readonly StyledProperty<bool> IsEditingProperty = AvaloniaProperty.Register<MarkdownTextBox, bool>(
        nameof(IsEditing));

    public string? Markdown
    {
        get => GetValue(MarkdownProperty);
        set => SetValue(MarkdownProperty, value);
    }
    
    public string? PlaceholderText
    {
        get => GetValue(PlaceholderTextProperty);
        set => SetValue(PlaceholderTextProperty, value);
    }
    
    public bool IsEditing
    {
        get => GetValue(IsEditingProperty);
        private set => SetValue(IsEditingProperty, value);
    }
    
    private MarkdownScrollViewer? _markdownLine;
    private TextBox? _textBox;

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _textBox?.LostFocus -= TextBoxFocusLost;
        
        _markdownLine = e.NameScope.Get<MarkdownScrollViewer>("PART_Markdown");
        _textBox = e.NameScope.Get<TextBox>("PART_TextBox");

        _textBox.LostFocus += TextBoxFocusLost;
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        TopLevel.GetTopLevel(this)?.InputPane?.StateChanged += InputPaneStateChanged;
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        TopLevel.GetTopLevel(this)?.InputPane?.StateChanged -= InputPaneStateChanged;
        base.OnDetachedFromVisualTree(e);
    }

    static MarkdownTextBox()
    {
        PointerPressedEvent.AddClassHandler<MarkdownTextBox>((box, args) => box.OnPointerPressed(args), RoutingStrategies.Tunnel);
    }
    
    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Console.WriteLine($"PointerPressed! {e.Route} -> {e.GetPosition(this)}, handled={e.Handled}, IsEditing={IsEditing}");

        if (!IsEditing)
        {
            IsEditing = true;
            e.Handled = true;
            
            Dispatcher.UIThread.Post(() =>
            {
                _textBox?.RaiseEvent(new PointerPressedEventArgs(
                    this,
                    e.Pointer,
                    VisualRoot,
                    e.GetPosition(VisualRoot),
                    e.Timestamp,
                    e.Properties,
                    e.KeyModifiers,
                    e.ClickCount
                ));
                _textBox?.Focus(NavigationMethod.Pointer);
                Console.WriteLine($"Raising new PointerPressed event: {e.Pointer.Id}, {e.GetPosition(_textBox)}, {e.Timestamp}, {e.Properties.IsLeftButtonPressed}, {e.KeyModifiers}");
            }, DispatcherPriority.Input);
        }
    }
    
    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (IsEditing && e.Key == Key.Escape)
        {
            IsEditing = false;
        }
    }

    private void TextBoxFocusLost(object? sender, FocusChangedEventArgs args)
    {
        Console.WriteLine($"TextBox LostFocus: was editing={IsEditing}, source={sender}, old={args.OldFocusedElement}, new={args.NewFocusedElement}");
            
        var menuItem = args.NewFocusedElement as MenuItem;
        var flyout = _textBox?.ContextFlyout as MenuFlyout;
        var isChildOfTextBox = _textBox?.ContextMenu?.Items.Contains(args.NewFocusedElement) ?? flyout?.Items.Contains(args.NewFocusedElement);
        if (isChildOfTextBox == false)
            IsEditing = false;

        Console.WriteLine($"menuItem: {menuItem}");
        Console.WriteLine($"ContextMenu: {_textBox?.ContextMenu}");
        Console.WriteLine($"ContextFlyout: {_textBox?.ContextFlyout}");
        Console.WriteLine($"Is child of textbox: {isChildOfTextBox}");
    }

    private void InputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        if (IsEditing && e.NewState == InputPaneState.Closed)
        {
            IsEditing = false;
        }
    }
}