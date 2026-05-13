using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Client.ViewModels;

public partial class RecordingViewModel : ObservableObject, IDisposable
{
    private readonly IRecordingService _recordingService;
    private readonly DispatcherTimer _durationTimer;
    private bool _disposed;

    [ObservableProperty] private bool _isRecording;
    [ObservableProperty] private bool _isPaused;
    [ObservableProperty] private string _durationText = "00:00:00";
    [ObservableProperty] private float _volumeLevel;

    public event Action<MeetingRecord>? RecordingStoppedWithFile;

    public RecordingViewModel(IRecordingService recordingService)
    {
        _recordingService = recordingService;

        _recordingService.VolumeChanged += OnVolumeChanged;

        _durationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _durationTimer.Tick += OnDurationTimerTick;
    }

    [RelayCommand]
    private async Task ToggleRecordingAsync()
    {
        try
        {
            if (IsRecording)
                await StopAsync();
            else
                await StartAsync();
        }
        catch (Exception ex)
        {
            LoggerService.Error("Recording toggle failed", ex);
        }
    }

    public Task StartAsync()
    {
        _recordingService.StartRecordingAsync();
        IsRecording = true;
        IsPaused = false;
        _durationTimer.Start();
        LoggerService.Info("Recording started via UI");
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        _durationTimer.Stop();
        var filePath = await _recordingService.StopRecordingAsync();
        IsRecording = false;
        IsPaused = false;
        RecordingStoppedWithFile?.Invoke(new MeetingRecord { FilePath = filePath });
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

    private void OnVolumeChanged(object? sender, float volume)
    {
        Dispatcher.UIThread.Post(() => VolumeLevel = volume);
    }

    private void OnDurationTimerTick(object? sender, EventArgs e)
    {
        DurationText = _recordingService.Duration.ToString(@"hh\:mm\:ss");
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _recordingService.VolumeChanged -= OnVolumeChanged;
            _durationTimer.Stop();
            _durationTimer.Tick -= OnDurationTimerTick;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}