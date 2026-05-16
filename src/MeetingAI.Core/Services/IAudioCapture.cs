namespace MeetingAI.Core.Services;

public interface IAudioCapture : IDisposable
{
    bool IsRecording { get; }
    event EventHandler<byte[]>? DataAvailable;
    event EventHandler<Exception?>? RecordingStopped;
    int SampleRate { get; }
    int Channels { get; }
    void StartRecording();
    void StopRecording();
}
