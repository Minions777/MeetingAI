using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public class TranscriptionService : ITranscriptionService
{
    private readonly ConfigurationService _configService;
    private readonly Dictionary<string, IAIProvider> _providers = new();
    
    public TranscriptionService(ConfigurationService configService)
    {
        _configService = configService;
        InitializeProviders();
    }
    
    private void InitializeProviders()
    {
        var settings = _configService.Load();
        foreach (var providerConfig in settings.Providers.Where(p => p.IsEnabled && p.SupportsTranscription))
        {
            try
            {
                var provider = ProviderFactory.Create(providerConfig);
                _providers[providerConfig.Id] = provider;
                LoggerService.Info($"Loaded transcription provider: {providerConfig.Name}");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"Failed to load provider {providerConfig.Name}", ex);
            }
        }
    }
    
    public async Task<Transcript> TranscribeAsync(
        string audioFilePath,
        string? providerId = null,
        TranscriptionOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken ct = default)
    {
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
        var bytes = await File.ReadAllBytesAsync(path);
        
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
}
