using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace TaskTwig.Views;

public partial class SleepView : UserControl
{
    public SleepView()
    {
        InitializeComponent();
    }
    
    public static readonly FuncValueConverter<bool, string> SleepButtonTextConverter =
        new(isSleeping => isSleeping ? "Wake Up" : "Go To Sleep");
}