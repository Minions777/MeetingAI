using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Services;

public interface IRecordingService : IDisposable
{
    bool IsRecording { get; }
    bool IsPaused { get; }
    TimeSpan Duration { get; }
    float CurrentVolume { get; }

    event EventHandler<float>? VolumeChanged;
    event EventHandler<string>? RecordingStarted;
    event EventHandler<string>? RecordingStopped;
    event EventHandler<Exception?>? RecordingError;

    Task StartRecordingAsync(RecordingOptions? options = null);
    Task<string> StopRecordingAsync();
    void Pause();
    void Resume();
}

public class RecordingOptions
{
    public string? OutputDirectory { get; set; }
    public bool IncludeMicrophone { get; set; } = true;
    public bool IncludeSystemAudio { get; set; } = true;
    public AudioQuality Quality { get; set; } = AudioQuality.High;
}
