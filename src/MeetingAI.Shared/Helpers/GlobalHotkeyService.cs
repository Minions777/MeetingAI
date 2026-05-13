#if WINDOWS
using System.Runtime.InteropServices;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Shared.Helpers;

/// <summary>
/// Windows global hotkey service using Win32 RegisterHotKey API.
/// Pure P/Invoke — no WPF dependency.
/// </summary>
public class WindowsHotkeyService : IPlatformHotkeyService
{
    #region Win32 API

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    private const int WM_HOTKEY = 0x0312;
    private const int GWL_WNDPROC = -4;
    private const uint MOD_NOREPEAT = 0x4000;

    #endregion

    private IntPtr _windowHandle;
    private IntPtr _oldWndProc;
    private WndProcDelegate? _wndProcDelegate; // prevent GC collection
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _currentId;
    private bool _disposed;

    public bool IsAvailable => true;

    /// <summary>
    /// Initialize with a native window handle (HWND).
    /// For Avalonia, pass the platform handle from the window.
    /// </summary>
    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;

        // Subclass the window to receive WM_HOTKEY messages
        _wndProcDelegate = WndProc;
        _oldWndProc = SetWindowLongPtr(_windowHandle, GWL_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));

        LoggerService.Info("Windows hotkey service initialized");
    }

    public bool RegisterHotkey(int id, KeyModifiers modifiers, string key, Action action)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            LoggerService.Error("Hotkey service not initialized (no window handle)");
            return false;
        }

        var winModifiers = ConvertModifiers(modifiers);
        var vk = ConvertKey(key);

        var result = RegisterHotKey(_windowHandle, id, winModifiers | MOD_NOREPEAT, vk);

        if (result)
        {
            _hotkeyActions[id] = action;
            LoggerService.Info($"Hotkey registered: {modifiers}+{key} (ID: {id})");
        }
        else
        {
            LoggerService.Warning($"Hotkey registration failed (ID: {id}), may be in use by another app");
        }

        return result;
    }

    public void UnregisterHotkey(int id)
    {
        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, id);
            _hotkeyActions.Remove(id);
        }
    }

    public void UnregisterAllHotkeys()
    {
        foreach (var id in _hotkeyActions.Keys.ToList())
            UnregisterHotkey(id);
    }

    public int GenerateHotkeyId() => ++_currentId;

    private static uint ConvertModifiers(KeyModifiers modifiers)
    {
        uint result = 0;
        if (modifiers.HasFlag(KeyModifiers.Alt)) result |= 0x0001;
        if (modifiers.HasFlag(KeyModifiers.Control)) result |= 0x0002;
        if (modifiers.HasFlag(KeyModifiers.Shift)) result |= 0x0004;
        if (modifiers.HasFlag(KeyModifiers.Win)) result |= 0x0008;
        return result;
    }

    private static uint ConvertKey(string key) => key.ToUpperInvariant() switch
    {
        "R" => 0x52,
        "S" => 0x53,
        _ => throw new ArgumentException($"Unsupported key: {key}")
    };

    private IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    LoggerService.Error("Hotkey callback failed", ex);
                }
            }
        }
        return CallWindowProc(_oldWndProc, hwnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAllHotkeys();

        // Restore original WndProc
        if (_windowHandle != IntPtr.Zero && _oldWndProc != IntPtr.Zero)
        {
            SetWindowLongPtr(_windowHandle, GWL_WNDPROC, _oldWndProc);
            _oldWndProc = IntPtr.Zero;
        }
        _wndProcDelegate = null;
    }
}

// Keep backward compatibility alias
public class GlobalHotkeyService : WindowsHotkeyService { }
#endif
