using System.ComponentModel;

namespace MeetingAI.Shared.i18n;

public class LocalizationManager : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public static LocalizationManager Instance { get; } = new();

    public string this[string key] => Get(key);

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
            ["SetAsDefault"] = "设为默认",
            ["RefreshProviders"] = "刷新列表",
            ["Pause"] = "暂停",
            ["Resume"] = "继续",
            ["Transcript"] = "转录内容",
            ["GenerateSummary"] = "生成摘要",
            ["MeetingSummary"] = "会议摘要",
            ["NoSummaryYet"] = "暂无摘要",
            ["RecordAndTranscribeHint"] = "录制并转录会议后，点击生成摘要",
            ["CopySummary"] = "复制摘要",
            ["RecordingHistory"] = "录音历史",
            ["NoRecordingHistory"] = "暂无录音记录",
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
            ["SetAsDefault"] = "Set Default",
            ["RefreshProviders"] = "Refresh",
            ["Pause"] = "Pause",
            ["Resume"] = "Resume",
            ["Transcript"] = "Transcript",
            ["GenerateSummary"] = "Generate Summary",
            ["MeetingSummary"] = "Meeting Summary",
            ["NoSummaryYet"] = "No summary yet",
            ["RecordAndTranscribeHint"] = "Record and transcribe a meeting, then click Generate Summary",
            ["CopySummary"] = "Copy Summary",
            ["RecordingHistory"] = "Recording History",
            ["NoRecordingHistory"] = "No recording history",
        }
    };

    private static string _currentLanguage = "zh-CN";

    public static string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_strings.ContainsKey(value))
            {
                _currentLanguage = value;
                Instance.NotifyAllPropertiesChanged();
            }
        }
    }

    private void NotifyAllPropertiesChanged()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
        foreach (var key in _strings[_currentLanguage].Keys)
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs($"Item[{key}]"));
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
