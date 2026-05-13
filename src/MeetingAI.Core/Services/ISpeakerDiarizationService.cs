using MeetingAI.Core.Models;

namespace MeetingAI.Core.Services;

/// <summary>
/// Defines the contract for speaker diarization services.
/// </summary>
public interface ISpeakerDiarizationService
{
    /// <summary>
    /// Performs speaker diarization on an audio file using the provided Whisper segments.
    /// </summary>
    /// <param name="audioFilePath">Path to the audio file.</param>
    /// <param name="whisperSegments">Word-level timestamps from Whisper transcription.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Diarization result with speaker assignments.</returns>
    Task<SpeakerDiarizationResult> ProcessAsync(
        string audioFilePath,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> whisperSegments,
        CancellationToken ct = default);

    /// <summary>
    /// Gets whether the diarization model is available.
    /// </summary>
    bool IsModelAvailable { get; }
}