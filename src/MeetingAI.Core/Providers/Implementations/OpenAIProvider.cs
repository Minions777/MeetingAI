using System.Net.Http.Headers;
using System.Text.Json;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Providers.Implementations;

public class OpenAIProvider : OpenAICompatibleProvider
{
    public override string Id => "openai";
    public override string Name => "OpenAI";
    public override AIProviderType ProviderType => AIProviderType.OpenAI;
    
    public override IReadOnlyList<string> SupportedChatModels { get; } = new[]
    {
        "gpt-4o", "gpt-4o-mini", "gpt-4-turbo", "gpt-3.5-turbo"
    };
    
    public override IReadOnlyList<string> SupportedTranscriptionModels { get; } = new[]
    {
        "whisper-1"
    };
    
    public override bool SupportsTranscription => true;
    public override bool SupportsChat => true;
    
    protected override void ConfigureHttpClient(HttpClient client)
    {
        // OpenAI 特定的配置（如果有）
    }
    
    public override async Task<Transcript> TranscribeAsync(AudioData audio, TranscriptionOptions? options = null, CancellationToken ct = default)
    {
        if (_httpClient == null || _config == null)
            throw new InvalidOperationException("Provider not configured");

        var endpoint = $"{_config.BaseUrl.TrimEnd('/')}/audio/transcriptions";

        using var content = new MultipartFormDataContent();
        var audioContent = CreateAudioContent(audio);
        audioContent.Headers.ContentType = new MediaTypeHeaderValue(GetAudioContentType(audio));
        content.Add(audioContent, "file", GetAudioFileName(audio));
        content.Add(new StringContent(_config.WhisperModel), "model");

        if (options?.Language != null)
            content.Add(new StringContent(options.Language), "language");

        if (options?.Prompt != null)
            content.Add(new StringContent(options.Prompt), "prompt");

        using var response = await _httpClient.PostAsync(endpoint, content, ct);
        var json = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            LoggerService.Error($"Whisper API Error: {response.StatusCode} - {json}");
            throw new HttpRequestException($"Whisper API Error: {response.StatusCode} - {json}");
        }

        using var doc = JsonDocument.Parse(json);
        var text = doc.RootElement.GetProperty("text").GetString() ?? "";

        var transcript = new Transcript
        {
            Text = text,
            Language = options?.Language ?? "zh",
            Duration = audio.Duration.TotalSeconds
        };

        // Parse segments if available
        if (doc.RootElement.TryGetProperty("segments", out var segments))
        {
            foreach (var seg in segments.EnumerateArray())
            {
                transcript.Segments.Add(new TranscriptSegment
                {
                    Id = seg.GetProperty("id").GetInt32(),
                    Start = TimeSpan.FromSeconds(seg.GetProperty("start").GetDouble()),
                    End = TimeSpan.FromSeconds(seg.GetProperty("end").GetDouble()),
                    Text = seg.GetProperty("text").GetString() ?? "",
                    Confidence = seg.TryGetProperty("confidence", out var conf) ? conf.GetDouble() : 1.0
                });
            }
        }

        return transcript;
    }

    private static HttpContent CreateAudioContent(AudioData audio)
    {
        if (!string.IsNullOrWhiteSpace(audio.FilePath))
        {
            var stream = new FileStream(
                audio.FilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                useAsync: true);

            return new StreamContent(stream);
        }

        return new ByteArrayContent(audio.Bytes);
    }

    private static string GetAudioContentType(AudioData audio)
    {
        return audio.Format.ToLowerInvariant() switch
        {
            "mp3" => "audio/mpeg",
            "m4a" => "audio/mp4",
            "webm" => "audio/webm",
            "ogg" => "audio/ogg",
            _ => "audio/wav"
        };
    }

    private static string GetAudioFileName(AudioData audio)
    {
        if (!string.IsNullOrWhiteSpace(audio.FilePath))
        {
            return Path.GetFileName(audio.FilePath);
        }

        var extension = string.IsNullOrWhiteSpace(audio.Format) ? "wav" : audio.Format;
        return $"recording.{extension.TrimStart('.')}";
    }
}
