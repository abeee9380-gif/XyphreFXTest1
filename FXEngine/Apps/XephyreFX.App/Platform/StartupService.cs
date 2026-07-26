using System.Diagnostics;

namespace XephyreFX.App.Platform;

/// <summary>
/// Registers/unregisters XephyreFX to launch automatically when the user logs in. Windows uses
/// the current-user Run registry key (via reg.exe, so no extra NuGet package is needed);
/// Linux uses a standard XDG autostart .desktop entry. Every operation is best-effort and
/// never throws -- a failure here should never crash the app.
/// </summary>
public sealed class StartupService
{
    private const string AppName = "XephyreFX";

    public bool IsEnabled()
    {
        try
        {
            if (OperatingSystem.IsWindows()) return IsWindowsStartupEnabled();
            if (OperatingSystem.IsLinux()) return File.Exists(LinuxAutostartPath);
        }
        catch
        {
            // Fall through to false below.
        }
        return false;
    }

    public bool SetEnabled(bool enabled)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                SetWindowsStartup(enabled);
                return true;
            }
            if (OperatingSystem.IsLinux())
            {
                SetLinuxStartup(enabled);
                return true;
            }
        }
        catch
        {
            // Best-effort -- caller just sees false and can tell the user it didn't take.
        }
        return false;
    }

    // --- Windows: current-user Run key, via reg.exe (avoids needing the Microsoft.Win32.Registry package) ---

    private const string WindowsRunKey = @"HKCU\Software\Microsoft\Windows\CurrentVersion\Run";

    private void SetWindowsStartup(bool enabled)
    {
        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        var psi = new ProcessStartInfo("reg.exe") { UseShellExecute = false, CreateNoWindow = true };

        if (enabled)
        {
            psi.ArgumentList.Add("add");
            psi.ArgumentList.Add(WindowsRunKey);
            psi.ArgumentList.Add("/v");
            psi.ArgumentList.Add(AppName);
            psi.ArgumentList.Add("/t");
            psi.ArgumentList.Add("REG_SZ");
            psi.ArgumentList.Add("/d");
            psi.ArgumentList.Add($"\"{exePath}\"");
            psi.ArgumentList.Add("/f");
        }
        else
        {
            psi.ArgumentList.Add("delete");
            psi.ArgumentList.Add(WindowsRunKey);
            psi.ArgumentList.Add("/v");
            psi.ArgumentList.Add(AppName);
            psi.ArgumentList.Add("/f");
        }

        using var proc = Process.Start(psi);
        proc?.WaitForExit(3000);
    }

    private bool IsWindowsStartupEnabled()
    {
        var psi = new ProcessStartInfo("reg.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        psi.ArgumentList.Add("query");
        psi.ArgumentList.Add(WindowsRunKey);
        psi.ArgumentList.Add("/v");
        psi.ArgumentList.Add(AppName);

        using var proc = Process.Start(psi);
        if (proc is null) return false;

        string output = proc.StandardOutput.ReadToEnd();
        proc.WaitForExit(3000);
        return proc.ExitCode == 0 && output.Contains(AppName, StringComparison.OrdinalIgnoreCase);
    }

    // --- Linux: XDG autostart .desktop entry ---

    private static string LinuxAutostartPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "autostart", "xephyrefx.desktop");

    private void SetLinuxStartup(bool enabled)
    {
        if (!enabled)
        {
            if (File.Exists(LinuxAutostartPath)) File.Delete(LinuxAutostartPath);
            return;
        }

        string? exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) return;

        var dir = Path.GetDirectoryName(LinuxAutostartPath);
        if (dir is not null) Directory.CreateDirectory(dir);

        string entry =
            "[Desktop Entry]\n" +
            "Type=Application\n" +
            $"Name={AppName}\n" +
            $"Exec=\"{exePath}\"\n" +
            "X-GNOME-Autostart-enabled=true\n";

        File.WriteAllText(LinuxAutostartPath, entry);
    }
}
