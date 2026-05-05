using MeetingAI.Core.Models;

namespace MeetingAI.Core.Providers.Abstractions;

public interface ITranscriptionProvider : IAIProvider
{
    Task<Transcript> TranscribeAsync(
        AudioData audio, 
        TranscriptionOptions? options = null, 
        IProgress<float>? progress = null,
        CancellationToken ct = default);
}
