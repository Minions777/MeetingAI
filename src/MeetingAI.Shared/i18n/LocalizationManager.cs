using System.Globalization;

namespace MeetingAI.Shared.i18n;

public class LocalizationManager
{
    private static readonly Dictionary<string, Dictionary<string, string>> _strings = new()
    {
        ["zh-CN"] = new Dictionary<string, string>
        {
            ["AppName"] = "会议助手",
            ["StartRecording"] = "开始录音",
            ["StopRecording"] = "停止录音",
            ["Transcribe"] = "转录",
            ["Summarize"] = "生成摘要",
            ["Copy"] = "复制",
            ["Settings"] = "设置",
            ["Provider"] = "AI Provider",
            ["ApiKey"] = "API Key",
            ["BaseUrl"] = "API 地址",
            ["Model"] = "模型",
            ["Save"] = "保存",
            ["Cancel"] = "取消",
            ["Test"] = "测试连接",
            ["Ready"] = "就绪",
            ["Recording"] = "录音中...",
            ["Transcribing"] = "转录中...",
            ["Summarizing"] = "生成摘要中...",
            ["Success"] = "成功",
            ["Error"] = "错误",
            ["Duration"] = "时长",
            ["NoRecording"] = "暂无录音",
            ["CopySuccess"] = "已复制到剪贴板",
            ["ConnectionSuccess"] = "连接成功",
            ["ConnectionFailed"] = "连接失败",
        },
        ["en-US"] = new Dictionary<string, string>
        {
            ["AppName"] = "MeetingAI",
            ["StartRecording"] = "Start Recording",
            ["StopRecording"] = "Stop Recording",
            ["Transcribe"] = "Transcribe",
            ["Summarize"] = "Summarize",
            ["Copy"] = "Copy",
            ["Settings"] = "Settings",
            ["Provider"] = "AI Provider",
            ["ApiKey"] = "API Key",
            ["BaseUrl"] = "API URL",
            ["Model"] = "Model",
            ["Save"] = "Save",
            ["Cancel"] = "Cancel",
            ["Test"] = "Test Connection",
            ["Ready"] = "Ready",
            ["Recording"] = "Recording...",
            ["Transcribing"] = "Transcribing...",
            ["Summarizing"] = "Summarizing...",
            ["Success"] = "Success",
            ["Error"] = "Error",
            ["Duration"] = "Duration",
            ["NoRecording"] = "No recording",
            ["CopySuccess"] = "Copied to clipboard",
            ["ConnectionSuccess"] = "Connection successful",
            ["ConnectionFailed"] = "Connection failed",
        }
    };
    
    private static string _currentLanguage = "zh-CN";
    
    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_strings.ContainsKey(value))
                _currentLanguage = value;
        }
    }
    
    public static string Get(string key)
    {
        if (_strings.TryGetValue(_currentLanguage, out var langStrings))
        {
            if (langStrings.TryGetValue(key, out var value))
                return value;
        }
        
        // Fallback to zh-CN
        if (_strings["zh-CN"].TryGetValue(key, out var fallback))
            return fallback;
            
        return key;
    }
    
    public static IReadOnlyList<string> AvailableLanguages => _strings.Keys.ToList();
}
