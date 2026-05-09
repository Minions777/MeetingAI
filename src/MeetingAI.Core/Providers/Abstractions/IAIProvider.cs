using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Providers.Abstractions;

public interface IAIProvider
{
    string Id { get; }
    string Name { get; }
    AIProviderType ProviderType { get; }
    IReadOnlyList<string> SupportedChatModels { get; }
    IReadOnlyList<string> SupportedTranscriptionModels { get; }
    bool IsConfigured { get; }
    bool SupportsTranscription { get; }
    bool SupportsChat { get; }

    void Configure(ProviderConfig config);
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamChatAsync(ChatRequest request, CancellationToken ct = default);
    Task<Transcript> TranscribeAsync(AudioData audio, TranscriptionOptions? options = null, CancellationToken ct = default);
    Task<bool> TestConnectionAsync(CancellationToken ct = default);
}
