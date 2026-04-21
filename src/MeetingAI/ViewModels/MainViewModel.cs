using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using MeetingAI.Models;
using MeetingAI.Services;
using Microsoft.Win32;

namespace MeetingAI.ViewModels;

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;
    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    { _execute = execute ?? throw new ArgumentNullException(nameof(execute)); _canExecute = canExecute; }
    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Func<object?, bool>? _canExecute;
    private int _isExecuting;
    public AsyncRelayCommand(Func<object?, Task> execute, Func<object?, bool>? canExecute = null)
    { _execute = execute ?? throw new ArgumentNullException(nameof(execute)); _canExecute = canExecute; }
    public bool CanExecute(object? parameter) => _isExecuting == 0 && (_canExecute?.Invoke(parameter) ?? true);
    public async void Execute(object? parameter)
    {
        if (Interlocked.CompareExchange(ref _isExecuting, 1, 0) == 1) return;
        try { CommandManager.InvalidateRequerySuggested(); await _execute(parameter); }
        catch (Exception ex) { LoggerService.Error("AsyncRelayCommand 执行失败", ex); }
        finally { _isExecuting = 0; CommandManager.InvalidateRequerySuggested(); }
    }
    public event EventHandler? CanExecuteChanged { add => CommandManager.RequerySuggested += value; remove => CommandManager.RequerySuggested -= value; }
}

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly AudioRecorderService _recorder;
    private readonly AISummaryService _aiService;
    private readonly TranscriptionService _transcriptionService;
    private readonly ConfigurationService _configService;
    private System.Windows.Threading.DispatcherTimer? _timer;
    private bool _disposed;

    public ObservableCollection<AIModelConfig> ModelConfigs { get; } = new();
    private AIModelConfig? _selectedConfig;
    public AIModelConfig? SelectedConfig { get => _selectedConfig; set { if (_selectedConfig != value) { _selectedConfig = value; OnPropertyChanged(); UpdateCommandStates(); SaveCurrentConfig(); } } }

    private bool _isRecording;
    public bool IsRecording { get => _isRecording; set { _isRecording = value; OnPropertyChanged(); UpdateCommandStates(); } }

    private string _recordingTime = "00:00:00";
    public string RecordingTime { get => _recordingTime; set { _recordingTime = value; OnPropertyChanged(); } }

    private float _volumeLevel;
    public float VolumeLevel { get => _volumeLevel; set { _volumeLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(VolumePercentage)); } }
    public int VolumePercentage => (int)(VolumeLevel * 100);

    private string _transcript = "";
    public string Transcript { get => _transcript; set { _transcript = value; OnPropertyChanged(); UpdateCommandStates(); } }

    private string _summary = "";
    public string Summary { get => _summary; set { _summary = value; OnPropertyChanged(); UpdateCommandStates(); } }

    private bool _isProcessing;
    public bool IsProcessing { get => _isProcessing; set { _isProcessing = value; OnPropertyChanged(); UpdateCommandStates(); } }

    private string _statusMessage = "准备就绪";
    public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }

    private string? _currentAudioPath;

    public bool CanStartRecording => !IsRecording && !IsProcessing;
    public bool CanStopRecording => IsRecording;
    public bool CanTranscribe => !string.IsNullOrEmpty(_currentAudioPath) && !IsProcessing;
    public bool CanSummarize => !string.IsNullOrEmpty(Transcript) && !IsProcessing && SelectedConfig != null;
    public bool CanCopy => !string.IsNullOrEmpty(Summary);

    public ICommand StartRecordingCommand { get; }
    public ICommand StopRecordingCommand { get; }
    public ICommand TranscribeCommand { get; }
    public ICommand SummarizeCommand { get; }
    public ICommand CopySummaryCommand { get; }
    public ICommand ImportAudioCommand { get; }
    public ICommand OpenSettingsCommand { get; }
    public ICommand SaveConfigsCommand { get; }

    private void UpdateCommandStates()
    {
        OnPropertyChanged(nameof(CanStartRecording));
        OnPropertyChanged(nameof(CanStopRecording));
        OnPropertyChanged(nameof(CanTranscribe));
        OnPropertyChanged(nameof(CanSummarize));
        OnPropertyChanged(nameof(CanCopy));
    }

    public MainViewModel()
    {
        LoggerService.Info("MainViewModel 初始化");
        _recorder = new AudioRecorderService();
        _aiService = new AISummaryService();
        _transcriptionService = new TranscriptionService();
        _configService = new ConfigurationService();

        var configs = _configService.LoadConfigs();
        foreach (var config in configs) ModelConfigs.Add(config);
        if (ModelConfigs.Count == 0) { var defaultConfig = AIModelConfig.CreateDefault(AIProvider.OpenAI); ModelConfigs.Add(defaultConfig); SaveCurrentConfig(); }
        var lastSelected = configs.FirstOrDefault(c => c.IsSelected);
        SelectedConfig = lastSelected ?? ModelConfigs.FirstOrDefault();

        StartRecordingCommand = new RelayCommand(StartRecording, _ => CanStartRecording);
        StopRecordingCommand = new RelayCommand(StopRecording, _ => CanStopRecording);
        TranscribeCommand = new AsyncRelayCommand(TranscribeAudioAsync, _ => CanTranscribe);
        SummarizeCommand = new AsyncRelayCommand(SummarizeAsync, _ => CanSummarize);
        CopySummaryCommand = new RelayCommand(CopyToClipboard, _ => CanCopy);
        ImportAudioCommand = new RelayCommand(ImportAudio);
        OpenSettingsCommand = new RelayCommand(OpenSettings);
        SaveConfigsCommand = new RelayCommand(_ => SaveAllConfigs());

        _recorder.VolumeLevelChanged += OnVolumeChanged;
        _recorder.RecordingSaved += OnRecordingSaved;
        _recorder.RecordingError += OnRecordingError;
        LoggerService.Info("MainViewModel 初始化完成");
    }

    private void OnVolumeChanged(object? sender, float level) => Application.Current?.Dispatcher.Invoke(() => VolumeLevel = level);
    private void OnRecordingSaved(object? sender, string path) => Application.Current?.Dispatcher.Invoke(() => { _currentAudioPath = path; StatusMessage = "录音已保存: " + Path.GetFileName(path); LoggerService.Info("录音已保存: " + path); });
    private void OnRecordingError(object? sender, Exception ex) => Application.Current?.Dispatcher.Invoke(() => { StatusMessage = "录音错误: " + GetUserFriendlyMessage(ex); LoggerService.Error("录音错误", ex); });

    private void StartRecording(object? _)
    {
        try
        {
            LoggerService.Info("开始录音");
            _recorder.StartRecording();
            IsRecording = true;
            StatusMessage = "正在录音...";
            _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }
        catch (Exception ex) { StatusMessage = "启动录音失败: " + GetUserFriendlyMessage(ex); LoggerService.Error("启动录音失败", ex); }
    }
    private void OnTimerTick(object? sender, EventArgs e) => RecordingTime = _recorder.RecordingDuration.ToString(@"hh:mm:ss");

    private void StopRecording(object? _) { LoggerService.Info("停止录音"); _timer?.Stop(); _timer = null; _recorder.StopRecording(); IsRecording = false; StatusMessage = "录音已停止"; }

    private async Task TranscribeAudioAsync(object? _)
    {
        if (string.IsNullOrEmpty(_currentAudioPath) || SelectedConfig == null) return;
        LoggerService.Info("开始转录: " + SelectedConfig.Model);
        IsProcessing = true; StatusMessage = "正在转录音频...";
        try { Transcript = await _transcriptionService.TranscribeAsync(_currentAudioPath, SelectedConfig); StatusMessage = "转录完成 (" + Transcript.Length + " 字符)"; LoggerService.Info("转录完成"); }
        catch (Exception ex) { StatusMessage = "转录失败: " + GetUserFriendlyMessage(ex); LoggerService.Error("转录失败", ex); }
        finally { IsProcessing = false; }
    }

    private async Task SummarizeAsync(object? _)
    {
        if (SelectedConfig == null || string.IsNullOrEmpty(Transcript)) return;
        LoggerService.Info("开始生成摘要: " + SelectedConfig.Model);
        IsProcessing = true; StatusMessage = "AI 正在生成摘要...";
        try { Summary = await _aiService.SummarizeAsync(Transcript, SelectedConfig); StatusMessage = "摘要生成完成"; LoggerService.Info("摘要生成完成"); }
        catch (Exception ex) { StatusMessage = "生成失败: " + GetUserFriendlyMessage(ex); LoggerService.Error("摘要生成失败", ex); }
        finally { IsProcessing = false; }
    }

    private void CopyToClipboard(object? _) { if (!string.IsNullOrEmpty(Summary)) { Clipboard.SetText(Summary); StatusMessage = "已复制到剪贴板"; LoggerService.Info("摘要已复制到剪贴板"); } }

    private void ImportAudio(object? _)
    {
        var dialog = new OpenFileDialog { Filter = "音频文件|*.mp3;*.wav;*.m4a;*.mp4;*.ogg;*.flac|所有文件|*.*", Title = "选择音频文件" };
        if (dialog.ShowDialog() == true) { _currentAudioPath = dialog.FileName; StatusMessage = "已导入: " + Path.GetFileName(dialog.FileName); LoggerService.Info("导入音频: " + dialog.FileName); }
    }

    private void OpenSettings(object? _)
    {
        if (SelectedConfig == null) return;
        LoggerService.Info("打开设置窗口");
        var settingsWindow = new Views.SettingsWindow { DataContext = SelectedConfig, Owner = Application.Current.MainWindow };
        var result = settingsWindow.ShowDialog();
        if (result == true) { SaveAllConfigs(); LoggerService.Info("配置已保存"); }
    }

    private void SaveCurrentConfig()
    {
        try { foreach (var config in ModelConfigs) config.IsSelected = (config == SelectedConfig); SaveAllConfigs(); }
        catch (Exception ex) { LoggerService.Error("保存配置失败", ex); }
    }

    private void SaveAllConfigs()
    {
        try { _configService.SaveConfigs(ModelConfigs.ToList()); StatusMessage = "配置已保存"; }
        catch (Exception ex) { StatusMessage = "保存配置失败"; LoggerService.Error("保存配置失败", ex); }
    }

    private string GetUserFriendlyMessage(Exception ex) => ex switch
    {
        HttpRequestException { StatusCode: System.Net.HttpStatusCode.Unauthorized } => "API Key 无效或已过期，请检查设置",
        HttpRequestException { StatusCode: System.Net.HttpStatusCode.NotFound } => "API 地址错误或服务不可用",
        HttpRequestException { StatusCode: System.Net.HttpStatusCode.TooManyRequests } => "请求过于频繁，请稍后再试",
        TimeoutException => "请求超时，请检查网络连接或增加超时时间",
        IOException => "文件读写错误，请检查文件权限",
        _ => ex.Message
    };

    public void Dispose()
    {
        if (_disposed) return;
        LoggerService.Info("MainViewModel 释放资源");
        if (_timer != null) { _timer.Stop(); _timer.Tick -= OnTimerTick; _timer = null; }
        _recorder.VolumeLevelChanged -= OnVolumeChanged;
        _recorder.RecordingSaved -= OnRecordingSaved;
        _recorder.RecordingError -= OnRecordingError;
        _recorder.Dispose(); _aiService.Dispose(); _transcriptionService.Dispose();
        SaveAllConfigs();
        _disposed = true; GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}