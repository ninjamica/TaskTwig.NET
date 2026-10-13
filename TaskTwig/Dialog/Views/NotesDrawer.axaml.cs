using System;
using Avalonia.Controls;
using Avalonia.Input;

namespace TaskTwig.Dialog.Views;

public partial class NotesDrawer : UserControl
{
    public NotesDrawer()
    {
        InitializeComponent();
    }
    
    private void NoteDragStarted(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse)
        {
            Console.WriteLine("Starting Note drag");
            e.PreventGestureRecognition();
        }
    }
}