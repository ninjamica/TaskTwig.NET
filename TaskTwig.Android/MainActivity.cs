using Android.App;
using Android.Content.PM;
using Avalonia.Android;

namespace TaskTwig.Android;

[Activity(
    Label = "TaskTwig",
    Theme = "@style/MyTheme.NoActionBar",
    Icon = "@drawable/tasktwig",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.UiMode)]
public class MainActivity : AvaloniaMainActivity
{
}