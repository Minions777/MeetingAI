using System.Collections.Concurrent;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;
using NAudio.Wave;

namespace MeetingAI.Core.Services;

public class TranscriptionService : ITranscriptionService, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly ConcurrentDictionary<string, IAIProvider> _providers = new();
    private readonly Lazy<Task> _initialization;
    private bool _disposed;

    public TranscriptionService(IConfigurationService configService)
    {
        _configService = configService;
        _configService.SettingsChanged += OnSettingsChanged;
        _initialization = new Lazy<Task>(() => Task.Run(InitializeProviders));
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        LoggerService.Info("Configuration changed, refreshing transcription providers...");
        // 使用 Task.Run 避免阻塞 UI 线程
        _ = Task.Run(RefreshProviders);
    }

    private void RefreshProviders()
    {
        // 清理旧的 Provider
        foreach (var provider in _providers.Values)
        {
            if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _providers.Clear();
        
        // 重新初始化
        InitializeProviders();
    }

    private void InitializeProviders()
    {
        var settings = _configService.Load();
        LoggerService.Info($"Initializing providers, found {settings.Providers.Count} providers");

        foreach (var providerConfig in settings.Providers.Where(p => p.IsEnabled && p.SupportsTranscription))
        {
            try
            {
                LoggerService.Info($"Creating provider: {providerConfig.Name}, API Key present: {!string.IsNullOrEmpty(providerConfig.ApiKey)}");
                var provider = ProviderFactory.Create(providerConfig);
                _providers[providerConfig.Id] = provider;
                LoggerService.Info($"Loaded transcription provider: {providerConfig.Name}, IsConfigured: {provider.IsConfigured}");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"Failed to load provider {providerConfig.Name}", ex);
            }
        }

        LoggerService.Info($"Total transcription providers loaded: {_providers.Count}");
    }
    
    public async Task<Transcript> TranscribeAsync(
        string audioFilePath,
        string? providerId = null,
        TranscriptionOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken ct = default)
    {
        // 确保 Provider 已初始化
        await _initialization.Value;
        
        var settings = _configService.Load();
        providerId ??= settings.DefaultProviderId;
        
        if (!_providers.TryGetValue(providerId, out var provider))
        {
            // Try to find a provider that supports transcription
            provider = _providers.Values.FirstOrDefault(p => p.SupportsTranscription);
            if (provider == null)
                throw new InvalidOperationException("No transcription provider available");
        }
        
        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException("Audio file not found", audioFilePath);
            
        var audioData = await LoadAudioFileAsync(audioFilePath);
        
        LoggerService.Info($"Transcribing with {provider.Name}: {audioFilePath}");
        progress?.Report(0.3f);
        
        var transcript = await provider.TranscribeAsync(audioData, options, ct);
        
        progress?.Report(1.0f);
        LoggerService.Info($"Transcription completed: {transcript.Text.Length} characters");
        
        return transcript;
    }
    
    private async Task<AudioData> LoadAudioFileAsync(string path)
    {
        // 使用流式读取，避免大文件一次性加载到内存
        byte[] bytes;
        using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true))
        {
            bytes = new byte[fileStream.Length];
            await fileStream.ReadAsync(bytes, 0, (int)fileStream.Length);
        }
        
        // Get duration using NAudio
        using var reader = new WaveFileReader(path);
        var duration = reader.TotalTime;
        
        return new AudioData
        {
            Bytes = bytes,
            Format = "wav",
            SampleRate = reader.WaveFormat.SampleRate,
            Channels = reader.WaveFormat.Channels,
            Duration = duration
        };
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
                _configService.SettingsChanged -= OnSettingsChanged;
                foreach (var provider in _providers.Values)
                {
                    if (provider is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                _providers.Clear();
            }
            _disposed = true;
        }
    }
}
