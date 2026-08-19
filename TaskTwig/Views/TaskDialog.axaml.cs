using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace TaskTwig.Views;

public partial class TaskDialog : UserControl
{
    public TaskDialog()
    {
        InitializeComponent();
    }
    
    public static readonly FuncValueConverter<int, string> WeekSpinnerTextConverter =
        new(counter => counter > 1 ? "Weeks" : "Week");
    
    public static readonly FuncValueConverter<int, string> MonthSpinnerTextConverter =
        new(counter => counter > 1 ? "Months" : "Month");
}