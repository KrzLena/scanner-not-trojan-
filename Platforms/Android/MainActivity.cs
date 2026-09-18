using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace MauiApp1;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.ScreenSize |
                           ConfigChanges.Orientation |
                           ConfigChanges.UiMode |
                           ConfigChanges.ScreenLayout |
                           ConfigChanges.SmallestScreenSize |
                           ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        if (Window == null)
            return;

        Window.AddFlags(
            WindowManagerFlags.DrawsSystemBarBackgrounds
        );

        Window.ClearFlags(
            WindowManagerFlags.TranslucentNavigation
        );

        Window.SetNavigationBarColor(
            Android.Graphics.Color.ParseColor("#302A32")
        );

        if (Build.VERSION.SdkInt >= BuildVersionCodes.R)
        {
            Window.InsetsController?.Show(
                WindowInsets.Type.NavigationBars()
            );

            
            Window.InsetsController?.SetSystemBarsAppearance(
                0,
                (int)WindowInsetsControllerAppearance.LightNavigationBars
            );
        }
    }
}