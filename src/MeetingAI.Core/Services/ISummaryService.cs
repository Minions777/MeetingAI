using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

public interface ISummaryService
{
    Task<Summary> SummarizeAsync(
        Transcript transcript,
        string? providerId = null,
        string? systemPrompt = null,
        CancellationToken ct = default);
}
