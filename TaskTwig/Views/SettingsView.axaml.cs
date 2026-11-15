using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace TaskTwig.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
    }
    
    public static readonly FuncValueConverter<bool, string> AccountButtonTextConverter =
        new(connected => connected ? "Sign Out" : "Sign In");
}