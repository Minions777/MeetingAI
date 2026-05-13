using MeetingAI.Shared.Logging;

namespace MeetingAI.Shared.Helpers;

public sealed class UnsupportedPlatformHotkeyService : IPlatformHotkeyService
{
    public bool IsAvailable => false;
    private int _hotkeyIdCounter;

    public int GenerateHotkeyId() => ++_hotkeyIdCounter;

    public bool RegisterHotkey(int id, KeyModifiers modifiers, string key, Action action)
    {
        LoggerService.Warning("Hotkey registration is not supported on this platform. Only Windows and macOS are supported.");
        return false;
    }

    public void UnregisterHotkey(int id) { }

    public void UnregisterAllHotkeys() { }

    public void Dispose() { }
}