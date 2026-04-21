using NAudio.Wave;
using System.IO;

namespace MeetingAI.Services;

public class AudioRecorderService : IDisposable
{
    private WasapiLoopbackCapture? _capture;
    private WaveFileWriter? _writer;
    private string _currentFilePath = "";
    private bool _isRecording;
    private DateTime _recordingStartTime;
    private readonly object _lock = new();

    public event EventHandler<float>? VolumeLevelChanged;
    public event EventHandler<string>? RecordingSaved;
    public event EventHandler<Exception>? RecordingError;

    public bool IsRecording => _isRecording;
    public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;

    public void StartRecording(string? outputPath = null)
    {
        lock (_lock)
        {
            if (_isRecording) return;

            outputPath ??= Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "MeetingAI", "Recordings");

            Directory.CreateDirectory(outputPath);
            _currentFilePath = Path.Combine(outputPath, $"meeting_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            try
            {
                _capture = new WasapiLoopbackCapture();
                _writer = new WaveFileWriter(_currentFilePath, _capture.WaveFormat);

                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;

                _recordingStartTime = DateTime.Now;
                _capture.StartRecording();
                _isRecording = true;
            }
            catch (Exception ex)
            {
                CleanupResources();
                RecordingError?.Invoke(this, ex);
                throw;
            }
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_writer == null) return;
        try
        {
            _writer.Write(e.Buffer, 0, e.BytesRecorded);
            float max = 0;
            for (int i = 0; i < e.BytesRecorded; i += 4)
            {
                float sample = Math.Abs(BitConverter.ToSingle(e.Buffer, i));
                if (sample > max) max = sample;
            }
            VolumeLevelChanged?.Invoke(this, max);
        }
        catch (Exception ex) { RecordingError?.Invoke(this, ex); }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null) RecordingError?.Invoke(this, e.Exception);
        CleanupResources();
        if (!string.IsNullOrEmpty(_currentFilePath) && File.Exists(_currentFilePath))
            RecordingSaved?.Invoke(this, _currentFilePath);
    }

    private void CleanupResources()
    {
        _writer?.Dispose(); _writer = null;
        _capture?.Dispose(); _capture = null;
    }

    public string StopRecording()
    {
        lock (_lock)
        {
            if (!_isRecording) return "";
            try { _capture?.StopRecording(); }
            catch (Exception ex) { RecordingError?.Invoke(this, ex); }
            finally { _isRecording = false; }
            return _currentFilePath;
        }
    }

    public void Dispose() { StopRecording(); CleanupResources(); }
}
