using MeetingAI.Core.Models;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

/// <summary>
/// Facade service for speaker diarization.
/// Provides a unified interface and manages the underlying implementation.
/// </summary>
public sealed class SpeakerDiarizationService : ISpeakerDiarizationService
{
    private readonly ISpeakerDiarizationService _innerService;

    public SpeakerDiarizationService(ISpeakerDiarizationService innerService)
    {
        _innerService = innerService;
    }

    public bool IsModelAvailable => _innerService.IsModelAvailable;

    public async Task<SpeakerDiarizationResult> ProcessAsync(
        string audioFilePath,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> whisperSegments,
        CancellationToken ct = default)
    {
        if (whisperSegments.Count == 0)
        {
            LoggerService.Warning("No whisper segments provided for diarization");
            return new SpeakerDiarizationResult
            {
                IsSuccess = true,
                Segments = new List<SpeakerSegment>()
            };
        }

        try
        {
            return await _innerService.ProcessAsync(audioFilePath, whisperSegments, ct);
        }
        catch (Exception ex)
        {
            LoggerService.Error("Speaker diarization failed", ex);
            return new SpeakerDiarizationResult
            {
                IsSuccess = false,
                ErrorMessage = ex.Message
            };
        }
    }
}