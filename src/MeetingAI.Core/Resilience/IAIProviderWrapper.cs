using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;

namespace MeetingAI.Core.Resilience;

public interface IAIProviderWrapper
{
    string ProviderName { get; }
    Task<ChatResponse> ChatAsync(ChatRequest request, CancellationToken ct = default);
    IAsyncEnumerable<string> StreamChatAsync(ChatRequest request, CancellationToken ct = default);
}