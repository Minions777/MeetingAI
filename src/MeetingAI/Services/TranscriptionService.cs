using System.IO;
using System.Net.Http;
using System.Text.Json;
using Microsoft.Win32;

namespace MeetingAI.Services;

public class TranscriptionService
{
    private readonly HttpClient _httpClient;

    public TranscriptionService()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10);
    }

    public async Task<string> TranscribeAsync(string filePath, AIModelConfig config)
    {
        // 支持 OpenAI Whisper API
        var endpoint = $"{config.BaseUrl}/audio/transcriptions";
        
        using var fileStream = File.OpenRead(filePath);
        using var content = new MultipartFormDataContent();
        
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/mpeg");
        content.Add(fileContent, "file", Path.GetFileName(filePath));
        content.Add(new StringContent(config.Model), "model");

        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {config.ApiKey}");

        var response = await _httpClient.PostAsync(endpoint, content);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Transcription Error: {responseBody}");

        using var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("text").GetString() ?? "";
    }
}