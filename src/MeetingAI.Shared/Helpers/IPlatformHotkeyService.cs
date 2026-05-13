namespace MeetingAI.Shared.Helpers;

public interface IPlatformHotkeyService : IDisposable
{
    bool RegisterHotkey(int id, KeyModifiers modifiers, string key, Action action);
    void UnregisterHotkey(int id);
    void UnregisterAllHotkeys();
    bool IsAvailable { get; }
}

[Flags]
public enum KeyModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8
}
