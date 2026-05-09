using NAudio.Wave;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Constants;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public class RecordingService : IRecordingService, IDisposable
{
    private WasapiLoopbackCapture? _capture;
    private WaveFileWriter? _writer;
    private string _currentFilePath = string.Empty;
    private DateTime _recordingStartTime;
    private TimeSpan _pausedDuration;
    private DateTime? _pauseStartTime;
    private bool _isPaused;
    private readonly object _lock = new();
    private bool _disposed;
    
    // Volume 节流相关
    private DateTime _lastVolumeUpdate = DateTime.MinValue;
    private readonly TimeSpan _volumeUpdateInterval = TimeSpan.FromMilliseconds(100); // 100ms 节流
    
    public bool IsRecording => _capture != null && !_isPaused;
    public bool IsPaused => _isPaused;
    
    public TimeSpan Duration
    {
        get
        {
            if (_capture == null) return TimeSpan.Zero;
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
            if (_capture != null)
                throw new InvalidOperationException("Already recording");
                
            options ??= new RecordingOptions();
            
            var outputDir = options.OutputDirectory ?? AppConstants.Paths.Recordings;
            Directory.CreateDirectory(outputDir);
            
            _currentFilePath = Path.Combine(outputDir, $"meeting_{DateTime.Now:yyyyMMdd_HHmmss}.wav");
            
            try
            {
                _capture = new WasapiLoopbackCapture();
                _writer = new WaveFileWriter(_currentFilePath, _capture.WaveFormat);
                
                _capture.DataAvailable += OnDataAvailable;
                _capture.RecordingStopped += OnRecordingStopped;
                
                _recordingStartTime = DateTime.Now;
                _pausedDuration = TimeSpan.Zero;
                _isPaused = false;
                
                _capture.StartRecording();
                
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
            if (_capture == null)
                throw new InvalidOperationException("Not recording");
                
            try
            {
                _capture.StopRecording();
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
            if (_capture == null || _isPaused) return;
            _isPaused = true;
            _pauseStartTime = DateTime.Now;
            LoggerService.Info("Recording paused");
        }
    }
    
    public void Resume()
    {
        lock (_lock)
        {
            if (_capture == null || !_isPaused || !_pauseStartTime.HasValue) return;
            
            _pausedDuration += DateTime.Now - _pauseStartTime.Value;
            _isPaused = false;
            _pauseStartTime = null;
            LoggerService.Info("Recording resumed");
        }
    }
    
    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        // 快速检查，避免不必要的锁竞争
        if (_writer == null || _isPaused) return;
        
        lock (_lock)
        {
            // 双重检查，防止在等待锁期间状态变化
            if (_writer == null || _isPaused) return;
            
            try
            {
                _writer.Write(e.Buffer, 0, e.BytesRecorded);
                
                // Calculate volume level (只在有足够数据时计算)
                float max = 0;
                if (e.BytesRecorded >= 4)
                {
                    for (int i = 0; i <= e.BytesRecorded - 4; i += 4)
                    {
                        var sample = BitConverter.ToSingle(e.Buffer, i);
                        if (sample > max) max = sample;
                    }
                }
                
                CurrentVolume = max;
                
                // 节流：限制 VolumeChanged 事件触发频率
                var now = DateTime.UtcNow;
                if (now - _lastVolumeUpdate >= _volumeUpdateInterval)
                {
                    _lastVolumeUpdate = now;
                    // 使用 BeginInvoke 避免阻塞音频回调线程
                    VolumeChanged?.BeginInvoke(this, max, null, null);
                }
            }
            catch (Exception ex)
            {
                RecordingError?.BeginInvoke(this, ex, null, null);
            }
        }
    }
    
    private void OnRecordingStopped(object? sender, StoppedEventArgs e)
    {
        lock (_lock)
        {
            if (e.Exception != null)
            {
                LoggerService.Error("Recording stopped due to error", e.Exception);
                RecordingError?.Invoke(this, e.Exception);
            }
            
            Cleanup();
            
            LoggerService.Info($"Recording stopped: {_currentFilePath}");
            RecordingStopped?.Invoke(this, _currentFilePath);
        }
    }
    
    private void Cleanup()
    {
        if (_writer != null)
        {
            _writer.Dispose();
            _writer = null;
        }
        
        if (_capture != null)
        {
            _capture.DataAvailable -= OnDataAvailable;
            _capture.RecordingStopped -= OnRecordingStopped;
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
