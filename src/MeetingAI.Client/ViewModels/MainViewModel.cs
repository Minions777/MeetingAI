using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.i18n;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Client.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IRecordingService _recordingService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly ISummaryService _summaryService;
    private readonly ConfigurationService _configService;
    private readonly DispatcherTimer _durationTimer;
    
    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _durationText = "00:00:00";
    [ObservableProperty] private float _volumeLevel;
    [ObservableProperty] private string _statusText = "";
    [ObservableProperty] private string _transcriptText = "";
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private MeetingRecord? _currentRecord;
    [ObservableProperty] private ProviderConfig? _selectedProvider;
    [ObservableProperty] private ObservableCollection<ProviderConfig> _providers = new();
    
    public MainViewModel(
        IRecordingService recordingService,
        ITranscriptionService transcriptionService,
        ISummaryService summaryService,
        ConfigurationService configService)
    {
        _recordingService = recordingService;
        _transcriptionService = transcriptionService;
        _summaryService = summaryService;
        _configService = configService;
        
        // Subscribe to recording events
        _recordingService.VolumeChanged += OnVolumeChanged;
        _recordingService.RecordingStopped += OnRecordingStopped;
        
        // Timer for duration updates
        _durationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };
        _durationTimer.Tick += (s, e) => UpdateDuration();
        
        LoadProviders();
        StatusText = LocalizationManager.Get("Ready");
    }
    
    private void LoadProviders()
    {
        var settings = _configService.Load();
        Providers.Clear();
        
        foreach (var provider in settings.Providers.Where(p => p.IsEnabled))
        {
            Providers.Add(provider);
        }
        
        SelectedProvider = Providers.FirstOrDefault(p => p.Id == settings.DefaultProviderId) 
                          ?? Providers.FirstOrDefault();
    }
    
    [RelayCommand]
    private async Task ToggleRecordingAsync()
    {
        try
        {
            if (IsRecording)
            {
                await StopRecordingAsync();
            }
            else
            {
                await StartRecordingAsync();
            }
        }
        catch (Exception ex)
        {
            LoggerService.Error("Recording toggle failed", ex);
            StatusText = $"{LocalizationManager.Get("Error")}: {ex.Message}";
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
        StatusText = LocalizationManager.Get("Recording");
        
        LoggerService.Info("Recording started via UI");
    }
    
    private async Task StopRecordingAsync()
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
        StatusText = LocalizationManager.Get("Success");
        
        LoggerService.Info($"Recording stopped: {filePath}");
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
        if (CurrentRecord == null || string.IsNullOrEmpty(CurrentRecord.FilePath))
        {
            StatusText = LocalizationManager.Get("NoRecording");
            return;
        }
        
        try
        {
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Transcribing;
            
            StatusText = LocalizationManager.Get("Transcribing");
            
            var transcript = await _transcriptionService.TranscribeAsync(
                CurrentRecord.FilePath,
                SelectedProvider?.Id);
            
            TranscriptText = transcript.Text;
            
            if (CurrentRecord != null)
            {
                CurrentRecord.Transcript = transcript;
                CurrentRecord.Status = RecordingStatus.Completed;
            }
            
            StatusText = LocalizationManager.Get("Success");
            LoggerService.Info("Transcription completed");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Transcription failed", ex);
            StatusText = $"{LocalizationManager.Get("Error")}: {ex.Message}";
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Failed;
        }
    }
    
    [RelayCommand]
    private async Task SummarizeAsync()
    {
        if (CurrentRecord?.Transcript == null)
        {
            StatusText = "请先转录";
            return;
        }
        
        try
        {
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Summarizing;
            
            StatusText = LocalizationManager.Get("Summarizing");
            
            var summary = await _summaryService.SummarizeAsync(
                CurrentRecord.Transcript,
                SelectedProvider?.Id);
            
            SummaryText = FormatSummary(summary);
            
            if (CurrentRecord != null)
            {
                CurrentRecord.Summary = summary;
                CurrentRecord.Status = RecordingStatus.Completed;
            }
            
            StatusText = LocalizationManager.Get("Success");
            LoggerService.Info("Summary generated");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Summarization failed", ex);
            StatusText = $"{LocalizationManager.Get("Error")}: {ex.Message}";
            if (CurrentRecord != null)
                CurrentRecord.Status = RecordingStatus.Failed;
        }
    }
    
    [RelayCommand]
    private void CopySummary()
    {
        if (string.IsNullOrEmpty(SummaryText)) return;
        
        Clipboard.SetText(SummaryText);
        StatusText = LocalizationManager.Get("CopySuccess");
    }
    
    [RelayCommand]
    private void OpenSettings()
    {
        var settingsWindow = new Views.SettingsWindow();
        settingsWindow.Owner = Application.Current.MainWindow;
        if (settingsWindow.ShowDialog() == true)
        {
            LoadProviders(); // Reload after settings change
        }
    }
    
    private void OnVolumeChanged(object? sender, float volume)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            VolumeLevel = volume;
        });
    }
    
    private void OnRecordingStopped(object? sender, string filePath)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Recording stopped event handler
        });
    }
    
    private void UpdateDuration()
    {
        var duration = _recordingService.Duration;
        DurationText = duration.ToString(@"hh\:mm\:ss");
    }
    
    private string FormatSummary(Summary summary)
    {
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
}
