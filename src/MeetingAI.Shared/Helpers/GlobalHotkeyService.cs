using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Shared.Helpers;

/// <summary>
/// 全局快捷键服务
/// 支持在应用未聚焦时捕获系统级快捷键
/// </summary>
public class GlobalHotkeyService : IDisposable
{
    #region Win32 API
    
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    
    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    
    private const int WM_HOTKEY = 0x0312;
    
    // Modifiers
    private const uint MOD_ALT = 0x0001;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_SHIFT = 0x0004;
    private const uint MOD_WIN = 0x0008;
    private const uint MOD_NOREPEAT = 0x4000;
    
    // Virtual Key Codes
    private const uint VK_R = 0x52;
    private const uint VK_S = 0x53;
    
    #endregion
    
    private IntPtr _windowHandle;
    private HwndSource? _source;
    private readonly Dictionary<int, Action> _hotkeyActions = new();
    private int _currentId = 0;
    private bool _isDisposed = false;
    
    /// <summary>
    /// 注册的热键定义
    /// </summary>
    public static class PredefinedHotkeys
    {
        public const int ToggleRecording = 1;  // Ctrl+Shift+R: 切换录音
        public const int StopRecording = 2;    // Ctrl+Shift+S: 停止录音
    }
    
    /// <summary>
    /// 初始化全局快捷键服务
    /// </summary>
    /// <param name="window">WPF Window 实例</param>
    public void Initialize(Window window)
    {
        var helper = new WindowInteropHelper(window);
        _windowHandle = helper.Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source?.AddHook(HwndHook);
        
        LoggerService.Info("全局快捷键服务已初始化");
    }
    
    /// <summary>
    /// 注册快捷键
    /// </summary>
    /// <param name="id">快捷键 ID</param>
    /// <param name="modifiers">修饰键 (MOD_ALT, MOD_CONTROL, MOD_SHIFT, MOD_WIN)</param>
    /// <param name="key">虚拟键码</param>
    /// <param name="action">快捷键触发时的回调</param>
    /// <returns>是否注册成功</returns>
    public bool RegisterHotkey(int id, uint modifiers, uint key, Action action)
    {
        if (_windowHandle == IntPtr.Zero)
        {
            LoggerService.Error("未初始化快捷键服务 (窗口句柄为空)");
            return false;
        }
        
        var result = RegisterHotKey(_windowHandle, id, modifiers | MOD_NOREPEAT, key);
        
        if (result)
        {
            _hotkeyActions[id] = action;
            var hotkeyName = GetHotkeyName(modifiers, key);
            LoggerService.Info($"快捷键注册成功: {hotkeyName} (ID: {id})");
        }
        else
        {
            LoggerService.Warning($"快捷键注册失败 (ID: {id})，可能已被其他应用占用");
        }
        
        return result;
    }
    
    /// <summary>
    /// 注册默认的录音快捷键
    /// - Ctrl+Shift+R: 切换录音
    /// - Ctrl+Shift+S: 停止录音
    /// </summary>
    public void RegisterDefaultHotkeys(Action toggleRecording, Action stopRecording)
    {
        // Ctrl+Shift+R: 切换录音
        RegisterHotkey(PredefinedHotkeys.ToggleRecording, MOD_CONTROL | MOD_SHIFT, VK_R, toggleRecording);
        
        // Ctrl+Shift+S: 停止录音
        RegisterHotkey(PredefinedHotkeys.StopRecording, MOD_CONTROL | MOD_SHIFT, VK_S, stopRecording);
    }
    
    /// <summary>
    /// 注销快捷键
    /// </summary>
    public void UnregisterHotkey(int id)
    {
        if (_windowHandle != IntPtr.Zero)
        {
            UnregisterHotKey(_windowHandle, id);
            _hotkeyActions.Remove(id);
            LoggerService.Debug($"快捷键已注销 (ID: {id})");
        }
    }
    
    /// <summary>
    /// 注销所有快捷键
    /// </summary>
    public void UnregisterAllHotkeys()
    {
        foreach (var id in _hotkeyActions.Keys.ToList())
        {
            UnregisterHotkey(id);
        }
    }
    
    /// <summary>
    /// 检查快捷键是否可用
    /// </summary>
    public static bool IsHotkeyAvailable(uint modifiers, uint key)
    {
        // 创建一个临时窗口来测试
        var tempWindow = new Window();
        var helper = new WindowInteropHelper(tempWindow);
        helper.EnsureHandle();
        
        var result = RegisterHotKey(helper.Handle, 99999, modifiers | MOD_NOREPEAT, key);
        
        if (result)
        {
            UnregisterHotKey(helper.Handle, 99999);
        }
        
        tempWindow.Close();
        return result;
    }
    
    /// <summary>
    /// 获取快捷键的人类可读名称
    /// </summary>
    public static string GetHotkeyName(uint modifiers, uint key)
    {
        var parts = new List<string>();
        
        if ((modifiers & MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & MOD_WIN) != 0) parts.Add("Win");
        
        parts.Add(((Key)key).ToString());
        
        return string.Join("+", parts);
    }
    
    /// <summary>
    /// WPF 消息处理
    /// </summary>
    private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            var id = wParam.ToInt32();
            
            if (_hotkeyActions.TryGetValue(id, out var action))
            {
                try
                {
                    LoggerService.Debug($"快捷键触发 (ID: {id})");
                    action.Invoke();
                    handled = true;
                }
                catch (Exception ex)
                {
                    LoggerService.Error("快捷键回调执行失败", ex);
                }
            }
        }
        
        return IntPtr.Zero;
    }
    
    /// <summary>
    /// 生成新的快捷键 ID
    /// </summary>
    public int GenerateHotkeyId()
    {
        return ++_currentId;
    }
    
    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                UnregisterAllHotkeys();
                _source?.RemoveHook(HwndHook);
                _source = null;
                LoggerService.Info("全局快捷键服务已释放");
            }
            
            _isDisposed = true;
        }
    }
    
    ~GlobalHotkeyService()
    {
        Dispose(false);
    }
}

/// <summary>
/// 快捷键修饰键枚举扩展
/// </summary>
[Flags]
public enum HotkeyModifiers : uint
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Win = 8,
    NoRepeat = 16384
}
