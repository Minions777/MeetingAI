using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using MeetingAI.Models;

namespace MeetingAI.Services;

public class TranscriptionService : IDisposable
{
    private readonly HttpClient _httpClient;
    private bool _disposed;

    private static readonly Dictionary<string, string> MediaTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".mp3", "audio/mpeg" }, { ".wav", "audio/wav" }, { ".m4a", "audio/mp4" },
        { ".mp4", "audio/mp4" }, { ".ogg", "audio/ogg" }, { ".webm", "audio/webm" }, { ".flac", "audio/flac" }
    };

    public TranscriptionService()
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    public async Task<string> TranscribeAsync(string filePath, AIModelConfig config)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(config);
        if (!File.Exists(filePath))
            throw new FileNotFoundException("音频文件不存在", filePath);

        var extension = Path.GetExtension(filePath);
        if (!MediaTypeMap.TryGetValue(extension, out var mediaType))
            throw new NotSupportedException($"不支持的音频格式: {extension}");

        var endpoint = $"{config.BaseUrl}/audio/transcriptions";
        using var fileStream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        content.Add(fileContent, "file", Path.GetFileName(filePath));
        content.Add(new StringContent(config.Model), "model");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = content;
        if (!string.IsNullOrEmpty(config.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKey);

        var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"转录失败 ({(int)response.StatusCode}): {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("text").GetString() ?? "";
    }

    public void Dispose()
    {
        if (_disposed) return;
        _httpClient.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
