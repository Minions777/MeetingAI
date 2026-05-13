using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

public interface ISummaryService
{
    Task<Summary> SummarizeAsync(
        Transcript transcript,
        string? providerId = null,
        string? systemPrompt = null,
        string? terminologyList = null,
        CancellationToken ct = default);

    IAsyncEnumerable<string> StreamSummarizeAsync(
        Transcript transcript,
        string? providerId = null,
        string? systemPrompt = null,
        string? terminologyList = null,
        CancellationToken ct = default);
}
