using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace TaskTwig.Views;

public partial class TodayView : UserControl
{
    public TodayView()
    {
        InitializeComponent();
    }
    
    public static readonly FuncMultiValueConverter<bool, string> TodaySleepButtonTextConverter = new(values =>
    {
        var isSleeping = values[0];
        var isDaytime = values[1];
        var hasRecordLastNight = values[2];

        if (isDaytime)
        {
            if (!isSleeping && !hasRecordLastNight)
                return "Missed Last Night";
            
            return isSleeping ? "Wake Up" : "Awake";
        }
        else
            return isSleeping ? "Sleeping" : "Go To Sleep";
    });
    
    public static readonly FuncMultiValueConverter<bool, bool> TodaySleepButtonEnabledConverter = new(values =>
    {
        var isSleeping = values[0];
        var isDaytime = values[1];

        return isDaytime ^ !isSleeping;
    });

    public static readonly FuncMultiValueConverter<bool, bool> TodayAddSleepButtonVisibleConverter = new(values =>
    {
        var isSleeping = values[0];
        var isDaytime = values[1];
        var hasRecordLastNight = values[2];

        return isDaytime && !isSleeping && !hasRecordLastNight;
    });
}