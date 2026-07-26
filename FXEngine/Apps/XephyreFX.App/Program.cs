using Avalonia;

namespace XephyreFX.App;

internal static class Program
{
    // Avalonia's bootstrap. This is the actual OS-level entry point on Windows, macOS, and Linux.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
}
