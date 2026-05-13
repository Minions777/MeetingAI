using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

public interface IAIAssistantService
{
    IAsyncEnumerable<string> AskAsync(
        string selectedText,
        string context,
        TimeSpan? timestamp = null,
        string? providerId = null,
        CancellationToken ct = default);

    Task<string> AskSingleAsync(
        string selectedText,
        string context,
        TimeSpan? timestamp = null,
        string? providerId = null,
        CancellationToken ct = default);
}