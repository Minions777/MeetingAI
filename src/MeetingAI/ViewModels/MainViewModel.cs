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
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public class MainViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly AudioRecorderService _recorder;
    private readonly AISummaryService _aiService;
    private readonly TranscriptionService _transcriptionService;
    private readonly ConfigurationService _configService;
    private System.Windows.Threading.DispatcherTimer? _timer;

    public ObservableCollection<AIModelConfig> ModelConfigs { get; } = new();
    public AIModelConfig? SelectedConfig { get; set; }

    private bool _isRecording = false;
    public bool IsRecording
    {
        get => _isRecording;
        set { _isRecording = value; OnPropertyChanged(); }
    }

    private string _recordingTime = "00:00:00";
    public string RecordingTime
    {
        get => _recordingTime;
        set { _recordingTime = value; OnPropertyChanged(); }
    }

    private float _volumeLevel = 0;
    public float VolumeLevel
    {
        get => _volumeLevel;
        set { _volumeLevel = value; OnPropertyChanged(); OnPropertyChanged(nameof(VolumePercentage)); }
    }

    public int VolumePercentage => (int)(VolumeLevel * 100);

    private string _transcript = "";
    public string Transcript
    {
        get => _transcript;
        set { _transcript = value; OnPropertyChanged(); }
    }

    private string _summary = "";
    public string Summary
    {
        get => _summary;
        set { _summary = value; OnPropertyChanged(); }
    }

    private bool _isProcessing = false;
    public bool IsProcessing
    {
        get => _isProcessing;
        set { _isProcessing = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "准备就绪";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private string? _currentAudioPath;

    public ICommand StartRecordingCommand { get; }
    public ICommand StopRecordingCommand { get; }
    public ICommand TranscribeCommand { get; }
    public ICommand SummarizeCommand { get; }
    public ICommand CopySummaryCommand { get; }
    public ICommand ImportAudioCommand { get; }
    public ICommand OpenSettingsCommand { get; }

    public MainViewModel()
    {
        _recorder = new AudioRecorderService();
        _aiService = new AISummaryService();
        _transcriptionService = new TranscriptionService();
        _configService = new ConfigurationService();

        var configs = _configService.LoadConfigs();
        foreach (var config in configs)
            ModelConfigs.Add(config);

        if (ModelConfigs.Count == 0)
            ModelConfigs.Add(new AIModelConfig { Name = "默认配置" });
      
        SelectedConfig = ModelConfigs.FirstOrDefault();

        StartRecordingCommand = new RelayCommand(_ => StartRecording(), _ => !IsRecording && !IsProcessing);
        StopRecordingCommand = new RelayCommand(_ => StopRecording(), _ => IsRecording);
        TranscribeCommand = new RelayCommand(_ => _ = TranscribeAudioAsync(), _ => !string.IsNullOrEmpty(_currentAudioPath) && !IsProcessing);
        SummarizeCommand = new RelayCommand(_ => _ = SummarizeAsync(), _ => !string.IsNullOrEmpty(Transcript) && !IsProcessing && SelectedConfig != null);
        CopySummaryCommand = new RelayCommand(_ => CopyToClipboard(), _ => !string.IsNullOrEmpty(Summary));
        ImportAudioCommand = new RelayCommand(_ => ImportAudio());
        OpenSettingsCommand = new RelayCommand(_ => OpenSettings());

        _recorder.VolumeLevelChanged += (s, level) => 
            Application.Current?.Dispatcher.Invoke(() => VolumeLevel = level);

        _recorder.RecordingSaved += (s, path) =>
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                _currentAudioPath = path;
                StatusMessage = $"录音已保存";
            });
        };
    }

    private void StartRecording()
    {
        _recorder.StartRecording();
        IsRecording = true;
        StatusMessage = "正在录音...";

        _timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (s, e) => RecordingTime = _recorder.RecordingDuration.ToString(@"hh:mm:ss");
        _timer.Start();
    }

    private void StopRecording()
    {
        _timer?.Stop();
        _recorder.StopRecording();
        IsRecording = false;
        StatusMessage = "录音已停止";
    }

    private async Task TranscribeAudioAsync()
    {
        if (string.IsNullOrEmpty(_currentAudioPath) || SelectedConfig == null)
            return;

        IsProcessing = true;
        StatusMessage = "正在转录...";

        try
        {
            Transcript = await _transcriptionService.TranscribeAsync(_currentAudioPath, SelectedConfig);
            StatusMessage = "转录完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"转录失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private async Task SummarizeAsync()
    {
        if (SelectedConfig == null || string.IsNullOrEmpty(Transcript))
            return;

        IsProcessing = true;
        StatusMessage = "正在生成摘要...";

        try
        {
            Summary = await _aiService.SummarizeAsync(Transcript, SelectedConfig);
            StatusMessage = "摘要生成完成";
        }
        catch (Exception ex)
        {
            StatusMessage = $"生成失败: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private void CopyToClipboard()
    {
        if (!string.IsNullOrEmpty(Summary))
        {
            Clipboard.SetText(Summary);
            StatusMessage = "已复制到剪贴板 ✓";
        }
    }

    private void ImportAudio()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "音频文件|*.mp3;*.wav;*.m4a;*.mp4|所有文件|*.*",
            Title = "选择音频文件"
        };

        if (dialog.ShowDialog() == true)
        {
            _currentAudioPath = dialog.FileName;
            StatusMessage = $"已导入: {Path.GetFileName(dialog.FileName)}";
        }
    }

    private void OpenSettings()
    {
        var settingsWindow = new Views.SettingsWindow();
        settingsWindow.DataContext = SelectedConfig;
        settingsWindow.ShowDialog();
    }

    public void Dispose()
    {
        _timer?.Stop();
        _recorder.Dispose();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}