#if MACOS
using System.Runtime.InteropServices;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Shared.Helpers;

/// <summary>
/// macOS global hotkey service using CGEventTap.
/// Requires Accessibility permissions (System Settings > Privacy & Security > Accessibility).
/// </summary>
public class MacHotkeyService : IPlatformHotkeyService
{
    private readonly Dictionary<int, (KeyModifiers Modifiers, string Key, Action Callback)> _hotkeys = new();
    private IntPtr _eventTap;
    private IntPtr _runLoopSource;
    private GCHandle _gcHandle;
    private bool _disposed;

    private static readonly Dictionary<string, ushort> KeyMap = new()
    {
        ["A"] = 0x00,
        ["B"] = 0x0B,
        ["C"] = 0x08,
        ["D"] = 0x02,
        ["E"] = 0x0E,
        ["F"] = 0x03,
        ["G"] = 0x05,
        ["H"] = 0x04,
        ["I"] = 0x22,
        ["J"] = 0x26,
        ["K"] = 0x28,
        ["L"] = 0x25,
        ["M"] = 0x2E,
        ["N"] = 0x2D,
        ["O"] = 0x1F,
        ["P"] = 0x23,
        ["Q"] = 0x0C,
        ["R"] = 0x0F,
        ["S"] = 0x01,
        ["T"] = 0x11,
        ["U"] = 0x20,
        ["V"] = 0x09,
        ["W"] = 0x0D,
        ["X"] = 0x07,
        ["Y"] = 0x10,
        ["Z"] = 0x06,
    };

    public bool IsAvailable => CheckAccessibility();
    private int _hotkeyIdCounter;

    public int GenerateHotkeyId() => ++_hotkeyIdCounter;

    public bool RegisterHotkey(int id, KeyModifiers modifiers, string key, Action action)
    {
        _hotkeys[id] = (modifiers, key.ToUpperInvariant(), action);

        if (_eventTap == IntPtr.Zero)
        {
            if (!InstallEventTap())
                return false;
        }

        LoggerService.Info($"Registered macOS hotkey {id}: {modifiers}+{key}");
        return true;
    }

    public void UnregisterHotkey(int id)
    {
        _hotkeys.Remove(id);
        LoggerService.Info($"Unregistered macOS hotkey {id}");

        if (_hotkeys.Count == 0 && _eventTap != IntPtr.Zero)
        {
            RemoveEventTap();
        }
    }

    public void UnregisterAllHotkeys()
    {
        _hotkeys.Clear();
        RemoveEventTap();
    }

    private bool InstallEventTap()
    {
        try
        {
            _gcHandle = GCHandle.Alloc(this);

            const ulong keyDownMask = 1 << 10; // kCGEventKeyDown

            _eventTap = CGEventTapCreate(
                0, // kCGSessionEventTap
                0, // kCGHeadInsertEventTap
                0, // kCGEventTapOptionDefault
                keyDownMask,
                EventTapCallback,
                GCHandle.ToIntPtr(_gcHandle));

            if (_eventTap == IntPtr.Zero)
            {
                LoggerService.Error("Failed to create CGEventTap. Check Accessibility permissions.");
                _gcHandle.Free();
                return false;
            }

            _runLoopSource = CFMachPortCreateRunLoopSource(IntPtr.Zero, _eventTap, 0);
            CFRunLoopAddSource(CFRunLoopGetMain(), _runLoopSource, new IntPtr(0x19)); // kCFRunLoopCommonModes
            CGEventTapEnable(_eventTap, true);

            LoggerService.Info("CGEventTap installed for global hotkeys");
            return true;
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to install event tap", ex);
            return false;
        }
    }

    private void RemoveEventTap()
    {
        if (_eventTap != IntPtr.Zero)
        {
            CGEventTapEnable(_eventTap, false);
            if (_runLoopSource != IntPtr.Zero)
            {
                CFRunLoopRemoveSource(CFRunLoopGetMain(), _runLoopSource, new IntPtr(0x19));
                CFRelease(_runLoopSource);
                _runLoopSource = IntPtr.Zero;
            }
            CFRelease(_eventTap);
            _eventTap = IntPtr.Zero;
        }
        if (_gcHandle.IsAllocated)
            _gcHandle.Free();
    }

    private static IntPtr EventTapCallback(IntPtr proxy, IntPtr type, IntPtr eventRef, IntPtr userData)
    {
        if (eventRef == IntPtr.Zero) return eventRef;

        try
        {
            var handle = GCHandle.FromIntPtr(userData);
            if (handle.Target is not MacHotkeyService service)
                return eventRef;

            var keyCode = CGEventGetIntegerValueField(eventRef, 53); // kCGKeyboardEventKeycode
            var flags = CGEventGetFlags(eventRef);

            var modifiers = KeyModifiers.None;
            if ((flags & 0x40000) != 0) modifiers |= KeyModifiers.Control;  // kCGEventFlagMaskControl
            if ((flags & 0x80000) != 0) modifiers |= KeyModifiers.Alt;      // kCGEventFlagMaskAlternate
            if ((flags & 0x20000) != 0) modifiers |= KeyModifiers.Shift;    // kCGEventFlagMaskShift
            if ((flags & 0x100000) != 0) modifiers |= KeyModifiers.Win;     // kCGEventFlagMaskCommand

            var keyName = KeyMap.FirstOrDefault(k => k.Value == (ushort)keyCode).Key;

            if (!string.IsNullOrEmpty(keyName))
            {
                foreach (var hotkey in service._hotkeys.Values)
                {
                    if (hotkey.Modifiers == modifiers && hotkey.Key == keyName)
                    {
                        hotkey.Callback?.Invoke();
                        return IntPtr.Zero; // consume the event
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LoggerService.Error("EventTap callback error", ex);
        }

        return eventRef;
    }

    private static bool CheckAccessibility()
    {
        try
        {
            return AXIsProcessTrusted();
        }
        catch (Exception ex)
        {
            LoggerService.Debug($"Accessibility trust check failed: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        UnregisterAllHotkeys();
    }

    #region P/Invoke

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern bool AXIsProcessTrusted();

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGEventTapCreate(int tap, int place, int options, ulong mask, CGEventTapCallbackDelegate callback, IntPtr userData);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGEventTapEnable(IntPtr tap, bool enable);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern long CGEventGetIntegerValueField(IntPtr eventRef, long field);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern ulong CGEventGetFlags(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFMachPortCreateRunLoopSource(IntPtr allocator, IntPtr port, long order);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern IntPtr CFRunLoopGetMain();

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopAddSource(IntPtr rl, IntPtr source, IntPtr mode);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRunLoopRemoveSource(IntPtr rl, IntPtr source, IntPtr mode);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    private delegate IntPtr CGEventTapCallbackDelegate(IntPtr proxy, IntPtr type, IntPtr eventRef, IntPtr userData);

    #endregion
}
#endif
