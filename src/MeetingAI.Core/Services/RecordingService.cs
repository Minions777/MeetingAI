using MeetingAI.Shared.Constants;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public class RecordingService : IRecordingService, IDisposable
{
    private readonly IAudioCapture _audioCapture;
    private Stream? _outputStream;
    private BinaryWriter? _writer;
    private string _currentFilePath = string.Empty;
    private DateTime _recordingStartTime;
    private TimeSpan _pausedDuration;
    private DateTime? _pauseStartTime;
    private bool _isPaused;
    private readonly object _lock = new();
    private bool _disposed;
    private long _dataBytesWritten;

    private DateTime _lastVolumeUpdate = DateTime.MinValue;
    private readonly TimeSpan _volumeUpdateInterval = TimeSpan.FromMilliseconds(100);

    public RecordingService(IAudioCapture audioCapture)
    {
        _audioCapture = audioCapture;
    }

    public bool IsRecording => _audioCapture.IsRecording && !_isPaused;
    public bool IsPaused => _isPaused;

    public TimeSpan Duration
    {
        get
        {
            if (!_audioCapture.IsRecording) return TimeSpan.Zero;
            if (_isPaused) return _pausedDuration;
            return DateTime.Now - _recordingStartTime + _pausedDuration;
        }
    }

    public float CurrentVolume { get; private set; }

    public event EventHandler<float>? VolumeChanged;
    public event EventHandler<string>? RecordingStarted;
    public event EventHandler<string>? RecordingStopped;
    public event EventHandler<Exception>? RecordingError;

    public Task StartRecordingAsync(RecordingOptions? options = null)
    {
        lock (_lock)
        {
            if (_audioCapture.IsRecording)
                throw new InvalidOperationException("Already recording");

            options ??= new RecordingOptions();

            var outputDir = options.OutputDirectory ?? AppConstants.Paths.Recordings;
            Directory.CreateDirectory(outputDir);

            _currentFilePath = Path.Combine(outputDir, $"meeting_{DateTime.Now:yyyyMMdd_HHmmss}.wav");

            try
            {
                _outputStream = File.Create(_currentFilePath);
                _writer = new BinaryWriter(_outputStream);
                WriteWavHeader(_writer, _audioCapture.SampleRate, _audioCapture.Channels, 16);
                _dataBytesWritten = 0;

                _audioCapture.DataAvailable += OnDataAvailable;
                _audioCapture.RecordingStopped += OnRecordingStopped;

                _recordingStartTime = DateTime.Now;
                _pausedDuration = TimeSpan.Zero;
                _isPaused = false;

                _audioCapture.StartRecording();

                LoggerService.Info($"Recording started: {_currentFilePath}");
                RecordingStarted?.Invoke(this, _currentFilePath);
            }
            catch (Exception ex)
            {
                Cleanup();
                LoggerService.Error("Failed to start recording", ex);
                RecordingError?.Invoke(this, ex);
                throw;
            }
        }

        return Task.CompletedTask;
    }

    public Task<string> StopRecordingAsync()
    {
        lock (_lock)
        {
            if (!_audioCapture.IsRecording)
                throw new InvalidOperationException("Not recording");

            try
            {
                _audioCapture.StopRecording();
            }
            catch (Exception ex)
            {
                LoggerService.Error("Error stopping recording", ex);
                Cleanup();
                throw;
            }
        }

        return Task.FromResult(_currentFilePath);
    }

    public void Pause()
    {
        lock (_lock)
        {
            if (!_audioCapture.IsRecording || _isPaused) return;
            _isPaused = true;
            _pauseStartTime = DateTime.Now;
            LoggerService.Info("Recording paused");
        }
    }

    public void Resume()
    {
        lock (_lock)
        {
            if (!_audioCapture.IsRecording || !_isPaused || !_pauseStartTime.HasValue) return;

            _pausedDuration += DateTime.Now - _pauseStartTime.Value;
            _isPaused = false;
            _pauseStartTime = null;
            LoggerService.Info("Recording resumed");
        }
    }

    private void OnDataAvailable(object? sender, byte[] data)
    {
        if (_writer == null || _isPaused) return;

        lock (_lock)
        {
            if (_writer == null || _isPaused) return;

            try
            {
                _writer.Write(data);
                _dataBytesWritten += data.Length;

                // Calculate volume level
                float max = 0;
                if (data.Length >= 4)
                {
                    for (int i = 0; i <= data.Length - 4; i += 4)
                    {
                        var sample = BitConverter.ToSingle(data, i);
                        if (sample > max) max = sample;
                    }
                }

                CurrentVolume = max;

                var now = DateTime.UtcNow;
                if (now - _lastVolumeUpdate >= _volumeUpdateInterval)
                {
                    _lastVolumeUpdate = now;
                    VolumeChanged?.BeginInvoke(this, max, null, null);
                }
            }
            catch (Exception ex)
            {
                RecordingError?.BeginInvoke(this, ex, null, null);
            }
        }
    }

    private void OnRecordingStopped(object? sender, Exception? ex)
    {
        lock (_lock)
        {
            if (ex != null)
            {
                LoggerService.Error("Recording stopped due to error", ex);
                RecordingError?.Invoke(this, ex);
            }

            FinalizeWavFile();
            Cleanup();

            LoggerService.Info($"Recording stopped: {_currentFilePath}");
            RecordingStopped?.Invoke(this, _currentFilePath);
        }
    }

    private void FinalizeWavFile()
    {
        if (_writer == null || _outputStream == null) return;

        try
        {
            // Update WAV header with final data size
            _outputStream.Seek(4, SeekOrigin.Begin);
            _writer.Write((uint)(36 + _dataBytesWritten));

            _outputStream.Seek(40, SeekOrigin.Begin);
            _writer.Write((uint)_dataBytesWritten);

            _writer.Flush();
        }
        catch (Exception ex)
        {
            LoggerService.Error("Failed to finalize WAV file", ex);
        }
    }

    private static void WriteWavHeader(BinaryWriter writer, int sampleRate, int channels, int bitsPerSample)
    {
        var byteRate = sampleRate * channels * bitsPerSample / 8;
        var blockAlign = (short)(channels * bitsPerSample / 8);

        writer.Write("RIFF"u8);
        writer.Write((uint)0); // Placeholder for file size
        writer.Write("WAVE"u8);
        writer.Write("fmt "u8);
        writer.Write((uint)16); // Subchunk1 size
        writer.Write((short)1); // PCM format
        writer.Write((short)channels);
        writer.Write((uint)sampleRate);
        writer.Write((uint)byteRate);
        writer.Write(blockAlign);
        writer.Write((short)bitsPerSample);
        writer.Write("data"u8);
        writer.Write((uint)0); // Placeholder for data size
    }

    private void Cleanup()
    {
        if (_writer != null)
        {
            _writer.Dispose();
            _writer = null;
        }

        if (_outputStream != null)
        {
            _outputStream.Dispose();
            _outputStream = null;
        }

        _audioCapture.DataAvailable -= OnDataAvailable;
        _audioCapture.RecordingStopped -= OnRecordingStopped;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Cleanup();
        _audioCapture.Dispose();
    }
}
