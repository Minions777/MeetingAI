using MeetingAI.Core.Models;

namespace MeetingAI.Core.Providers.Abstractions;

public interface IChatProvider : IAIProvider
{
    new IAsyncEnumerable<string> StreamChatAsync(ChatRequest request, CancellationToken ct = default);
}
