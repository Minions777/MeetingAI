using MeetingAI.Core.Models;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public class TranscriptionService : ITranscriptionService, IDisposable
{
    private readonly IConfigurationService _configService;
    private readonly ProviderCollection _providerCollection;

    public TranscriptionService(IConfigurationService configService)
    {
        _configService = configService;
        _providerCollection = new ProviderCollection(configService, p => p.SupportsTranscription);
    }

    public async Task<Transcript> TranscribeAsync(
        string audioFilePath,
        string? providerId = null,
        TranscriptionOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken ct = default)
    {
        var providers = await _providerCollection.GetProvidersAsync();

        var settings = _configService.Load();
        providerId ??= settings.DefaultProviderId;

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
        var fileInfo = new FileInfo(path);
        var format = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
        int sampleRate = 16000;
        int channels = 1;
        var duration = TimeSpan.Zero;

        // Parse WAV header if it's a WAV file
        if (format is "wav" or "wave" && fileInfo.Length >= 44)
        {
            try
            {
                using var fs = File.OpenRead(path);
                using var reader = new BinaryReader(fs);
                var riff = new string(reader.ReadChars(4));
                if (riff == "RIFF")
                {
                    reader.ReadInt32(); // file size
                    var wave = new string(reader.ReadChars(4));
                    if (wave == "WAVE")
                    {
                        // Find fmt chunk
                        while (fs.Position < fileInfo.Length - 8)
                        {
                            var chunkId = new string(reader.ReadChars(4));
                            var chunkSize = reader.ReadInt32();
                            if (chunkId == "fmt ")
                            {
                                reader.ReadInt16(); // format
                                channels = reader.ReadInt16();
                                sampleRate = reader.ReadInt32();
                                var byteRate = reader.ReadInt32();
                                reader.ReadInt16(); // block align
                                reader.ReadInt16(); // bits per sample
                                if (byteRate > 0)
                                    duration = TimeSpan.FromSeconds((double)(fileInfo.Length - 44) / byteRate);
                                break;
                            }
                            fs.Seek(chunkSize, SeekOrigin.Current);
                        }
                    }
                }
            }
            catch
            {
                // Fall back to defaults if parsing fails
            }
        }

        return new AudioData
        {
            FilePath = path,
            Length = fileInfo.Length,
            Format = format,
            SampleRate = sampleRate,
            Channels = channels,
            Duration = duration
        };
    }

    public void Dispose()
    {
        _providerCollection.Dispose();
        GC.SuppressFinalize(this);
    }
}
