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

    public event EventHandler<float>? VolumeLevelChanged;
    public event EventHandler<string>? RecordingSaved;

    public bool IsRecording => _isRecording;
    public TimeSpan RecordingDuration => _isRecording ? DateTime.Now - _recordingStartTime : TimeSpan.Zero;

    public void StartRecording(string? outputPath = null)
    {
        if (_isRecording) return;

        outputPath ??= Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "MeetingAI", "Recordings");

        Directory.CreateDirectory(outputPath);

        _currentFilePath = Path.Combine(outputPath, $"meeting_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

        _capture = new WasapiLoopbackCapture();
        _writer = new WaveFileWriter(_currentFilePath, _capture.WaveFormat);

        _capture.DataAvailable += (s, e) =>
        {
            _writer?.Write(e.Buffer, 0, e.BytesRecorded);
            
            float max = 0;
            for (int i = 0; i < e.BytesRecorded; i += 4)
            {
                float sample = Math.Abs(BitConverter.ToSingle(e.Buffer, i));
                if (sample > max) max = sample;
            }
            VolumeLevelChanged?.Invoke(this, max);
        };

        _capture.RecordingStopped += (s, e) =>
        {
            _writer?.Dispose();
            _writer = null;
            _capture?.Dispose();
            _capture = null;
            RecordingSaved?.Invoke(this, _currentFilePath);
        };

        _recordingStartTime = DateTime.Now;
        _capture.StartRecording();
        _isRecording = true;
    }

    public string StopRecording()
    {
        if (!_isRecording) return "";
        _capture?.StopRecording();
        _isRecording = false;
        return _currentFilePath;
    }

    public void Dispose()
    {
        StopRecording();
        _writer?.Dispose();
        _capture?.Dispose();
    }
}