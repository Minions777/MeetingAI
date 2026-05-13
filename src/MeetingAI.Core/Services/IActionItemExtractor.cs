using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

public interface IActionItemExtractor
{
    Task<IReadOnlyList<ActionItem>> ExtractAsync(string summaryText, CancellationToken cancellationToken = default);
}