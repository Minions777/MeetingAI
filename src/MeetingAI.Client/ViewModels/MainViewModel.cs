using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Client.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IRecordingService _recordingService;
    private bool _disposed;

    [ObservableProperty] private string _statusText = "就绪";
    [ObservableProperty] private MeetingRecord? _currentRecord;
    [ObservableProperty] private MeetingRecord? _selectedHistoryRecord;
    [ObservableProperty] private ObservableCollection<MeetingRecord> _meetingHistory = new();
    [ObservableProperty] private bool _hasSummary;
    [ObservableProperty] private string _summaryText = "";

    public RecordingViewModel Recording { get; }
    public ProviderViewModel Providers { get; }
    public HistoryViewModel History { get; }
    public SummaryViewModel Summary { get; }

    public MainViewModel(
        IRecordingService recordingService,
        ITranscriptionService transcriptionService,
        ISummaryService summaryService,
        IConfigurationService configService,
        MeetingHistoryService historyService)
    {
        _recordingService = recordingService;
        Recording = new RecordingViewModel(recordingService);
        Providers = new ProviderViewModel(configService);
        History = new HistoryViewModel(historyService);
        Summary = new SummaryViewModel(summaryService, configService, transcriptionService);

        Recording.RecordingStoppedWithFile += OnRecordingStoppedWithFile;
        _recordingService.RecordingStopped += OnServiceRecordingStopped;

        InitializeAsync();
    }

    private async void InitializeAsync()
    {
        await Providers.LoadProvidersAsync();
        await History.LoadRecentAsync();
    }

    private void OnRecordingStoppedWithFile(MeetingRecord record)
    {
        if (CurrentRecord != null)
        {
            CurrentRecord.FilePath = record.FilePath;
            CurrentRecord.Status = RecordingStatus.Completed;
            _ = History.SaveAsync(CurrentRecord);
        }
    }

    private void OnServiceRecordingStopped(object? sender, string filePath)
    {
        if (CurrentRecord != null)
        {
            CurrentRecord.FilePath = filePath;
            CurrentRecord.Status = RecordingStatus.Completed;
        }
    }

    [RelayCommand]
    private void LoadMeetingRecord(MeetingRecord? record)
    {
        if (record == null) return;

        SelectedHistoryRecord = record;
        Summary.TranscriptText = record.Transcript?.Text ?? "";
        SummaryText = Summary.FormatSummary(record.Summary);
        HasSummary = record.Summary != null;
        CurrentRecord = record;

        StatusText = $"已加载: {record.Title}";
    }

    [RelayCommand]
    private async Task DeleteMeetingRecord(MeetingRecord? record)
    {
        if (record == null) return;

        await History.DeleteAsync(record);
        MeetingHistory = History.MeetingHistory;
        StatusText = "录音已删除";
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
                Recording.RecordingStoppedWithFile -= OnRecordingStoppedWithFile;
                _recordingService.RecordingStopped -= OnServiceRecordingStopped;
                Recording.Dispose();
            }
            _disposed = true;
        }
    }
}