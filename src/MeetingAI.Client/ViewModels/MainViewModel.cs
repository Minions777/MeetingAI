using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Core.Services;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Client.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IRecordingService _recordingService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ISummaryService _summaryService;
    private readonly IConfigurationService _configService;
    private readonly MeetingHistoryService _historyService;
    private readonly DispatcherTimer _durationTimer;
    private bool _disposed;

    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _durationText = "00:00:00";
    [ObservableProperty] private float _volumeLevel;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _transcriptText = "";
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private bool _hasSummary;
    [ObservableProperty] private bool _isStreaming;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private MeetingRecord? _currentRecord;
    [ObservableProperty] private MeetingRecord? _selectedHistoryRecord;
    [ObservableProperty] private ProviderConfig? _selectedProvider;
    [ObservableProperty] private ObservableCollection<ProviderConfig> _providers = new();
    [ObservableProperty] private ObservableCollection<MeetingRecord> _meetingHistory = new();

    public MainViewModel(
        IRecordingService recordingService,
        ITranscriptionService transcriptionService,
        ISummaryService summaryService,
        IConfigurationService configService,
        MeetingHistoryService historyService)
    {
        _recordingService = recordingService;
        _transcriptionService = transcriptionService;
        _summaryService = summaryService;
        _configService = configService;
        _historyService = historyService;

        _recordingService.VolumeChanged += OnVolumeChanged;
        _recordingService.RecordingStopped += OnRecordingStopped;

        _durationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _durationTimer.Tick += OnDurationTimerTick;

        StatusText = "就绪";

        Dispatcher.UIThread.Post(async () =>
        {
            await LoadProvidersAsync();
            await LoadMeetingHistoryAsync();
        }, DispatcherPriority.Background);
    }

    private async Task LoadProvidersAsync()
    {
        await Task.Run(() =>
        {
            var settings = _configService.Load();

            Dispatcher.UIThread.Post(() =>
            {
                Providers.Clear();

                foreach (var provider in settings.Providers.Where(p => p.IsEnabled))
                {
                    Providers.Add(provider);
                }

                SelectedProvider = Providers.FirstOrDefault(p => p.Id == settings.DefaultProviderId)
                                  ?? Providers.FirstOrDefault();
            });
        });
    }

    [RelayCommand]
    private async Task ToggleRecordingAsync()
    {
        LoggerService.Info($"ToggleRecordingAsync called. IsRecording={IsRecording}");
        try
        {
            if (IsRecording)
            {
                LoggerService.Info("Stopping recording...");
                await StopRecordingAsync();
            }
            else
            {
                LoggerService.Info("Starting recording...");
                await StartRecordingAsync();
            }
        }
        catch (Exception ex)
        {
            LoggerService.Error("Recording toggle failed", ex);
            StatusText = $"错误: {ex.Message}";
        }
    }

    private async Task StartRecordingAsync()
    {
        CurrentRecord = new MeetingRecord
        {
            Title = $"会议_{DateTime.Now:yyyyMMdd_HHmmss}",
            Status = RecordingStatus.Recording
        };

        await _recordingService.StartRecordingAsync();

        IsRecording = true;
        IsPaused = false;
        _durationTimer.Start();
        StatusText = "录音中...";

        LoggerService.Info("Recording started via UI");
    }

    private async Task StopRecordingAsync()
    {
        try
        {
            _durationTimer.Stop();
            var filePath = await _recordingService.StopRecordingAsync();

            if (CurrentRecord != null)
            {
                CurrentRecord.FilePath = filePath;
                CurrentRecord.Status = RecordingStatus.Completed;
            }

            IsRecording = false;
            IsPaused = false;
            StatusText = "成功";

            LoggerService.Info($"Recording stopped: {filePath}");
        }
        catch (Exception ex)
        {
            _durationTimer.Stop();
            IsRecording = false;
            IsPaused = false;
            LoggerService.Error("Failed to stop recording", ex);
            throw;
        }
    }

    [RelayCommand]
    private void PauseResume()
    {
        if (IsPaused)
        {
            _recordingService.Resume();
            IsPaused = false;
        }
        else
        {
            _recordingService.Pause();
            IsPaused = true;
        }
    }

    [RelayCommand]
    private async Task TranscribeAsync()
    {
        if (IsProcessing || CurrentRecord == null || string.IsNullOrEmpty(CurrentRecord.FilePath))
        {
            if (!IsProcessing)
                StatusText = "暂无录音";
            return;
        }

        try
        {
            IsProcessing = true;
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Transcribing;

            StatusText = "转录中...";

            var transcript = await _transcriptionService.TranscribeAsync(
                CurrentRecord!.FilePath,
                SelectedProvider?.Id);

            TranscriptText = transcript.Text;

            if (CurrentRecord != null)
            {
                CurrentRecord.Transcript = transcript;
                CurrentRecord.Status = RecordingStatus.Completed;
            }

            StatusText = "成功";
            LoggerService.Info("Transcription completed");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Transcription failed", ex);
            StatusText = $"错误: {ex.Message}";
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Failed;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task SummarizeAsync()
    {
        if (IsProcessing || CurrentRecord?.Transcript == null)
        {
            if (!IsProcessing)
                StatusText = "请先转录";
            return;
        }

        try
        {
            IsProcessing = true;
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Summarizing;

            StatusText = "生成摘要中...";

            var summary = await _summaryService.SummarizeAsync(
                CurrentRecord!.Transcript!,
                SelectedProvider?.Id);

            SummaryText = FormatSummary(summary);
            HasSummary = !string.IsNullOrWhiteSpace(SummaryText);

            if (CurrentRecord != null)
            {
                CurrentRecord.Summary = summary;
                CurrentRecord.Status = RecordingStatus.Completed;
            }

            StatusText = "成功";
            LoggerService.Info("Summary generated");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Summarization failed", ex);
            StatusText = $"错误: {ex.Message}";
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Failed;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task StreamSummarizeAsync()
    {
        if (IsProcessing || CurrentRecord?.Transcript == null)
        {
            if (!IsProcessing)
                StatusText = "请先转录";
            return;
        }

        try
        {
            IsProcessing = true;
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Summarizing;

            IsStreaming = true;
            SummaryText = string.Empty;
            StatusText = "生成摘要中...";

            var settings = _configService.Load();
            var providerId = SelectedProvider?.Id ?? settings.DefaultProviderId;

            var providerConfig = settings.Providers.FirstOrDefault(p => p.Id == providerId);
            if (providerConfig == null)
            {
                providerConfig = settings.Providers.FirstOrDefault(p => p.IsEnabled && p.SupportsChat);
                if (providerConfig == null)
                {
                    StatusText = "没有可用的 AI Provider";
                    return;
                }
            }

            var provider = ProviderFactory.Create(providerConfig);

            var request = new ChatRequest
            {
                Model = providerConfig.Model,
                SystemPrompt = SummaryService.DefaultSummaryPrompt,
                Messages = new List<ChatMessage>
                {
                    new ChatMessage { Role = "user", Content = $"请总结以下会议记录：\n\n{CurrentRecord!.Transcript!.Text}" }
                }
            };

            var sb = new System.Text.StringBuilder();
            await foreach (var chunk in provider.StreamChatAsync(request, CancellationToken.None))
            {
                sb.Append(chunk);
                SummaryText = sb.ToString();
            }

            HasSummary = !string.IsNullOrWhiteSpace(SummaryText);

            if (CurrentRecord != null)
            {
                CurrentRecord.Summary = new Summary { Overview = SummaryText };
                CurrentRecord.Status = RecordingStatus.Completed;
            }

            StatusText = "成功";
            LoggerService.Info("Streaming summary completed");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Streaming summarization failed", ex);
            StatusText = $"错误: {ex.Message}";
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Failed;
        }
        finally
        {
            IsStreaming = false;
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task CopySummaryAsync()
    {
        if (string.IsNullOrEmpty(SummaryText)) return;

        var topLevel = TopLevel.GetTopLevel(
            (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow);
        if (topLevel?.Clipboard != null)
        {
            await topLevel.Clipboard.SetTextAsync(SummaryText);
        }
        StatusText = "已复制到剪贴板";
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        if (mainWindow == null) return;

        var settingsWindow = new Views.SettingsWindow();
        var result = await settingsWindow.ShowDialog<bool?>(mainWindow);
        if (result == true)
        {
            await LoadProvidersAsync();
        }
    }

    [RelayCommand]
    private async Task SetAsDefaultProvider()
    {
        if (SelectedProvider == null) return;

        await Task.Run(() =>
        {
            var settings = _configService.Load();
            settings.DefaultProviderId = SelectedProvider.Id;
            _configService.Save(settings);
        });

        StatusText = $"默认 Provider 已设置为: {SelectedProvider.Name}";
        LoggerService.Info($"Default provider set to: {SelectedProvider.Name}");
    }

    [RelayCommand]
    private async Task ReloadProviders()
    {
        await LoadProvidersAsync();
        StatusText = "Provider 列表已刷新";
    }

    private async Task LoadMeetingHistoryAsync()
    {
        try
        {
            var history = await _historyService.GetRecentAsync(10);
            MeetingHistory.Clear();
            foreach (var record in history)
            {
                MeetingHistory.Add(record);
            }
            LoggerService.Info($"Loaded {MeetingHistory.Count} meeting records from history");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to load meeting history", ex);
        }
    }

    private async Task SaveCurrentMeetingAsync()
    {
        if (CurrentRecord == null) return;

        try
        {
            await _historyService.SaveAsync(CurrentRecord);
            _ = LoadMeetingHistoryAsync();
            LoggerService.Info($"Meeting saved to history: {CurrentRecord.Title}");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to save meeting", ex);
        }
    }

    [RelayCommand]
    private void LoadMeetingRecord(MeetingRecord? record)
    {
        if (record == null) return;

        SelectedHistoryRecord = record;
        TranscriptText = record.Transcript?.Text ?? "";
        SummaryText = FormatSummary(record.Summary);
        HasSummary = record.Summary != null;
        CurrentRecord = record;

        StatusText = $"已加载: {record.Title}";
    }

    [RelayCommand]
    private async Task DeleteMeetingRecord(MeetingRecord? record)
    {
        if (record == null) return;

        try
        {
            await _historyService.DeleteAsync(record.Id);
            MeetingHistory.Remove(record);
            StatusText = "录音已删除";
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to delete meeting", ex);
            StatusText = "删除失败";
        }
    }

    private void OnVolumeChanged(object? sender, float volume)
    {
        Dispatcher.UIThread.Post(() =>
        {
            VolumeLevel = volume;
        });
    }

    private void OnRecordingStopped(object? sender, string filePath)
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (CurrentRecord != null)
            {
                CurrentRecord.FilePath = filePath;
                CurrentRecord.Status = RecordingStatus.Completed;
                _ = SaveCurrentMeetingAsync();
            }
        });
    }

    private void OnDurationTimerTick(object? sender, EventArgs e)
    {
        UpdateDuration();
    }

    private void UpdateDuration()
    {
        var duration = _recordingService.Duration;
        DurationText = duration.ToString(@"hh\:mm\:ss");
    }

    private string FormatSummary(Summary? summary)
    {
        if (summary == null) return string.Empty;

        var sb = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(summary.Overview))
            sb.AppendLine($"**会议概要**: {summary.Overview}");

        if (summary.KeyPoints.Any())
        {
            sb.AppendLine("\n**关键要点**:");
            foreach (var point in summary.KeyPoints)
                sb.AppendLine($"  • {point}");
        }

        if (summary.ActionItems.Any())
        {
            sb.AppendLine("\n**行动项**:");
            foreach (var item in summary.ActionItems)
                sb.AppendLine($"  • {item}");
        }

        if (summary.Decisions.Any())
        {
            sb.AppendLine("\n**决议**:");
            foreach (var decision in summary.Decisions)
                sb.AppendLine($"  • {decision}");
        }

        return sb.ToString();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _recordingService.VolumeChanged -= OnVolumeChanged;
                _recordingService.RecordingStopped -= OnRecordingStopped;

                _durationTimer.Stop();
                _durationTimer.Tick -= OnDurationTimerTick;
            }
            _disposed = true;
        }
    }
}
