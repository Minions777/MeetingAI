using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

public interface ITranscriptionService
{
    Task<Transcript> TranscribeAsync(
        string audioFilePath,
        string? providerId = null,
        TranscriptionOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken ct = default);
}
