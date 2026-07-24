using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace XephyreFX.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var runtime = new SharedRuntime();
            var sceneWindow = new SceneWindow(runtime);
            var settingsWindow = new SettingsWindow(runtime, sceneWindow);

            // Closing the settings window is what actually quits the app; the scene window is
            // secondary and gets shown alongside it.
            desktop.MainWindow = settingsWindow;
            sceneWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
