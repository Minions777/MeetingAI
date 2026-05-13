#if WINDOWS
using NAudio.Wave;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public class WindowsAudioCapture : IAudioCapture
{
    private WasapiLoopbackCapture? _capture;
    private bool _disposed;

    public bool IsRecording => _capture != null;
    public int SampleRate { get; private set; } = 48000;
    public int Channels { get; private set; } = 2;

    public event EventHandler<byte[]>? DataAvailable;
    public event EventHandler<Exception>? RecordingStopped;

    public void StartRecording()
    {
        if (_capture != null)
            throw new InvalidOperationException("Already recording");

        _capture = new WasapiLoopbackCapture();
        SampleRate = _capture.WaveFormat.SampleRate;
        Channels = _capture.WaveFormat.Channels;

        _capture.DataAvailable += OnDataAvailable;
        _capture.RecordingStopped += OnCaptureStopped;
        _capture.StartRecording();

        LoggerService.Info($"Windows audio capture started: {SampleRate}Hz, {Channels}ch");
    }

    public void StopRecording()
    {
        if (_capture == null) return;

        try
        {
            _capture.StopRecording();
        }
        catch (Exception ex)
        {
            LoggerService.Error("Error stopping Windows audio capture", ex);
            RecordingStopped?.Invoke(this, ex);
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded > 0)
        {
            var data = new byte[e.BytesRecorded];
            Buffer.BlockCopy(e.Buffer, 0, data, 0, e.BytesRecorded);
            DataAvailable?.Invoke(this, data);
        }
    }

    private void OnCaptureStopped(object? sender, StoppedEventArgs e)
    {
        var ex = e.Exception;
        if (ex != null)
            LoggerService.Error("Windows audio capture stopped with error", ex);

        Cleanup();
        RecordingStopped?.Invoke(this, ex!);
    }

    private void Cleanup()
    {
        if (_capture != null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnCaptureStopped;
            _capture.Dispose();
            _capture = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cleanup();
    }
}
#endif
