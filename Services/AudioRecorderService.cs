using NAudio.Wave;
using System.IO;

namespace MeetingAI.Services;

public class AudioRecorderService : IDisposable
{
    private WasapiLoopbackCapture? _capture;
    private WaveFileWriter? _writer;
    private string _currentFilePath = "";
    private bool _isRecording = false;
    private DateTime _recordingStartTime;
    private readonly object _lock = new();

    public event EventHandler<float>? VolumeLevelChanged;
    public event EventHandler<string>? RecordingSaved;
    public event EventHandler<string>? RecordingError;

    public bool IsRecording => _isRecording;
    public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;
    public string? CurrentFilePath => _currentFilePath;

    public bool StartRecording(string? outputPath = null)
    {
        lock (_lock)
        {
            if (_isRecording) return false;

            try
            {
                outputPath ??= Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "MeetingAI",
                    "Recordings");

                Directory.CreateDirectory(outputPath);

                _currentFilePath = Path.Combine(outputPath, 
                    $"meeting_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

                _capture = new WasapiLoopbackCapture();
                _writer = new WaveFileWriter(_currentFilePath, _capture.WaveFormat);

                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;

                _recordingStartTime = DateTime.Now;
                _capture.StartRecording();
                _isRecording = true;
                return true;
            }
            catch (Exception ex)
            {
                RecordingError?.Invoke(this, ex.Message);
                Cleanup();
                return false;
            }
        }
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (_writer == null || e.BytesRecorded == 0) return;

        try
        {
            _writer.Write(e.Buffer, 0, e.BytesRecorded);
            
            float max = 0;
            for (int i = 0; i < e.BytesRecorded; i += 4)
            {
                if (i + 4 <= e.BytesRecorded)
                {
                    float sample = Math.Abs(BitConverter.ToSingle(e.Buffer, i));
                    if (sample > max) max = sample;
                }
            }
            
            float normalizedLevel = Math.Min(1.0f, max * 2);
            VolumeLevelChanged?.Invoke(this, normalizedLevel);
        }
        catch
        {
        }
    }

    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            RecordingError?.Invoke(this, e.Exception.Message);
        }
        
        Cleanup();
        
        if (File.Exists(_currentFilePath))
        {
            RecordingSaved?.Invoke(this, _currentFilePath);
        }
    }

    private void Cleanup()
    {
        _writer?.Dispose();
        _writer = null;
        _capture?.Dispose();
        _capture = null;
        _isRecording = false;
    }

    public string StopRecording()
    {
        lock (_lock)
        {
            if (!_isRecording) return "";

            try
            {
                _capture?.StopRecording();
            }
            catch
            {
            }

            var path = _currentFilePath;
            _isRecording = false;
            return path;
        }
    }

    public void Dispose()
    {
        StopRecording();
        Cleanup();
    }
}