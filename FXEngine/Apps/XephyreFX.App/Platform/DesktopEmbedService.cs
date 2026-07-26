using System.Runtime.InteropServices;

namespace XephyreFX.App.Platform;

/// <summary>
/// Reparents a window behind the desktop icons (the "WorkerW" trick every Rainmeter/Lively-style
/// tool uses on Windows). This is the single most experimental piece of the whole app -- it
/// pokes at undocumented-but-widely-relied-upon Windows Explorer internals via P/Invoke, and
/// there is no equivalent on Linux, so every entry point here is a no-op there.
///
/// Explorer's behavior here has genuinely changed across Windows versions/updates, so this
/// retries a few times before giving up. There used to be a fallback that attached directly to
/// Progman when no WorkerW was found -- removed, because Progman sits *behind* the wallpaper,
/// so that "succeeded" while actually making the window invisible. An honest failure is more
/// useful than a silent one.
/// </summary>
public static class DesktopEmbedService
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll")]
    private static extern IntPtr GetParent(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private const uint WM_SPAWN_WORKERW = 0x052C;

    private static IntPtr _lastWorkerW = IntPtr.Zero;

    /// <summary>
    /// Reparents <paramref name="windowHandle"/> behind the desktop icons. Returns whether it
    /// succeeded, plus a human-readable reason either way.
    /// </summary>
    public static async Task<(bool success, string reason)> EmbedBehindDesktopIconsAsync(IntPtr windowHandle)
    {
        if (!OperatingSystem.IsWindows()) return (false, "Desktop mode is Windows-only.");
        if (windowHandle == IntPtr.Zero) return (false, "Window handle wasn't available yet -- try again in a second.");

        try
        {
            IntPtr workerW = await FindWorkerWAsync();

            if (workerW == IntPtr.Zero)
            {
                return (false, "Couldn't find the desktop's WorkerW window after several tries. " +
                                "This can happen on some Windows builds/updates -- staying a normal window.");
            }

            SetParent(windowHandle, workerW);
            _lastWorkerW = workerW;
            return (true, "Attached behind the desktop icons.");
        }
        catch (Exception ex)
        {
            return (false, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// True if the window is still actually parented to a live WorkerW. Used by a watchdog to
    /// notice when Explorer restarts (which invalidates the old WorkerW handle) so the caller
    /// can re-embed instead of silently ending up detached/invisible.
    /// </summary>
    public static bool IsStillAttached(IntPtr windowHandle)
    {
        if (!OperatingSystem.IsWindows() || windowHandle == IntPtr.Zero) return false;

        try
        {
            IntPtr currentParent = GetParent(windowHandle);
            return currentParent != IntPtr.Zero && IsWindow(currentParent) && currentParent == _lastWorkerW;
        }
        catch
        {
            return false;
        }
    }

    private static async Task<IntPtr> FindWorkerWAsync()
    {
        IntPtr progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return IntPtr.Zero;

        IntPtr workerW = IntPtr.Zero;

        // Explorer doesn't always spawn the WorkerW synchronously with the message send, so
        // this asks a few times with a short wait in between rather than giving up after one try.
        for (int attempt = 0; attempt < 4 && workerW == IntPtr.Zero; attempt++)
        {
            SendMessageTimeout(progman, WM_SPAWN_WORKERW, IntPtr.Zero, IntPtr.Zero, 0, 1000, out _);
            await Task.Delay(250);

            EnumWindows((hwnd, _) =>
            {
                IntPtr shellView = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellView != IntPtr.Zero)
                {
                    // The WorkerW we want is the *next* one after the one hosting the icons.
                    workerW = FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);
                }
                return true; // keep enumerating
            }, IntPtr.Zero);

            // Newer Windows builds sometimes nest WorkerW directly under Progman instead of
            // as a top-level sibling -- try that shape too before retrying the whole loop.
            if (workerW == IntPtr.Zero)
            {
                workerW = FindWindowEx(progman, IntPtr.Zero, "WorkerW", null);
            }
        }

        return workerW;
    }

    /// <summary>Detaches the window from the desktop back to being a normal top-level window.</summary>
    public static void Detach(IntPtr windowHandle)
    {
        if (!OperatingSystem.IsWindows() || windowHandle == IntPtr.Zero) return;

        try
        {
            SetParent(windowHandle, IntPtr.Zero);
            _lastWorkerW = IntPtr.Zero;
        }
        catch
        {
            // Best-effort -- worst case the window just stays wherever it was.
        }
    }
}
