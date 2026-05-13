using MeetingAI.Shared.Logging;

namespace MeetingAI.Shared.Helpers;

public sealed class UnsupportedPlatformHotkeyService : IPlatformHotkeyService
{
    public bool IsAvailable => false;

    public bool RegisterHotkey(int id, KeyModifiers modifiers, string key, Action action)
    {
        LoggerService.Warning("Hotkey registration is not supported on this platform. Only Windows and macOS are supported.");
        return false;
    }

    public void UnregisterHotkey(int id) { }

    public void UnregisterAllHotkeys() { }

    public void Dispose() { }
}