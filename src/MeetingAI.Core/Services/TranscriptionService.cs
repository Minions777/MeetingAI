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
    private readonly object _providersLock = new();
    private IReadOnlyDictionary<string, IAIProvider> _providers = new Dictionary<string, IAIProvider>();
    private readonly Lazy<Task> _initialization;
    private bool _disposed;

    public TranscriptionService(IConfigurationService configService)
    {
        _configService = configService;
        _configService.SettingsChanged += OnSettingsChanged;
        _initialization = new Lazy<Task>(() => Task.Run(RefreshProviders));
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        LoggerService.Info("Configuration changed, refreshing transcription providers...");
        _ = Task.Run(RefreshProviders);
    }

    private void RefreshProviders()
    {
        var providers = CreateProviders();
        ReplaceProviders(providers);
    }

    private IReadOnlyDictionary<string, IAIProvider> CreateProviders()
    {
        var settings = _configService.Load();
        var providers = new Dictionary<string, IAIProvider>();
        LoggerService.Info($"Initializing providers, found {settings.Providers.Count} providers");

        foreach (var providerConfig in settings.Providers.Where(p => p.IsEnabled && p.SupportsTranscription))
        {
            try
            {
                LoggerService.Info($"Creating provider: {providerConfig.Name}, API Key present: {!string.IsNullOrEmpty(providerConfig.ApiKey)}");
                var provider = ProviderFactory.Create(providerConfig);
                providers[providerConfig.Id] = provider;
                LoggerService.Info($"Loaded transcription provider: {providerConfig.Name}, IsConfigured: {provider.IsConfigured}");
            }
            catch (Exception ex)
            {
                LoggerService.Error($"Failed to load provider {providerConfig.Name}", ex);
            }
        }

        LoggerService.Info($"Total transcription providers loaded: {providers.Count}");
        return providers;
    }

    public async Task<Transcript> TranscribeAsync(
        string audioFilePath,
        string? providerId = null,
        TranscriptionOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken ct = default)
    {
        await _initialization.Value;

        var settings = _configService.Load();
        providerId ??= settings.DefaultProviderId;

        var providers = _providers;
        if (!providers.TryGetValue(providerId, out var provider))
        {
            provider = providers.Values.FirstOrDefault(p => p.SupportsTranscription);
            if (provider == null)
                throw new InvalidOperationException("No transcription provider available");
        }

        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException("Audio file not found", audioFilePath);

        var audioData = LoadAudioFile(audioFilePath);

        LoggerService.Info($"Transcribing with {provider.Name}: {audioFilePath}");
        progress?.Report(0.3f);

        var transcript = await provider.TranscribeAsync(audioData, options, ct);

        progress?.Report(1.0f);
        LoggerService.Info($"Transcription completed: {transcript.Text.Length} characters");

        return transcript;
    }

    private static AudioData LoadAudioFile(string path)
    {
        using var reader = new WaveFileReader(path);
        var duration = reader.TotalTime;
        var fileInfo = new FileInfo(path);

        return new AudioData
        {
            FilePath = path,
            Length = fileInfo.Length,
            Format = Path.GetExtension(path).TrimStart('.').ToLowerInvariant(),
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
                DisposeProviders(ReplaceProviders(new Dictionary<string, IAIProvider>()));
            }
            _disposed = true;
        }
    }

    private IReadOnlyDictionary<string, IAIProvider> ReplaceProviders(IReadOnlyDictionary<string, IAIProvider> providers)
    {
        lock (_providersLock)
        {
            var oldProviders = _providers;
            _providers = providers;
            return oldProviders;
        }
    }

    private static void DisposeProviders(IReadOnlyDictionary<string, IAIProvider> providers)
    {
        foreach (var provider in providers.Values)
        {
            if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
