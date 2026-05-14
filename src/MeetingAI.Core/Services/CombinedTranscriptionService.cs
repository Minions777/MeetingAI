using MeetingAI.Core.Models;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

/// <summary>
/// Combined transcription service that merges Whisper transcription with speaker diarization.
/// </summary>
public interface ICombinedTranscriptionService
{
    Task<Transcript> TranscribeWithSpeakerDiarizationAsync(
        string audioFilePath,
        string? providerId = null,
        TranscriptionOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Combined transcription service that merges Whisper transcription with speaker diarization.
/// </summary>
public sealed class CombinedTranscriptionService : ICombinedTranscriptionService
{
    private readonly ITranscriptionService _transcriptionService;
    private readonly ISpeakerDiarizationService _diarizationService;

    public CombinedTranscriptionService(
        ITranscriptionService transcriptionService,
        ISpeakerDiarizationService diarizationService)
    {
        _transcriptionService = transcriptionService;
        _diarizationService = diarizationService;
    }

    /// <summary>
    /// Transcribes audio and assigns speaker IDs to each segment.
    /// </summary>
    public async Task<Transcript> TranscribeWithSpeakerDiarizationAsync(
        string audioFilePath,
        string? providerId = null,
        TranscriptionOptions? options = null,
        IProgress<float>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report(0.1f);

        // Step 1: Run Whisper transcription
        var transcript = await _transcriptionService.TranscribeAsync(
            audioFilePath, providerId, options, progress, ct);

        progress?.Report(0.7f);

        // Step 2: Run speaker diarization if model is available
        if (_diarizationService.IsModelAvailable)
        {
            var whisperSegments = transcript.Segments
                .Select(s => (s.Start, s.End))
                .ToList();

            var diarizationResult = await _diarizationService.ProcessAsync(
                audioFilePath, whisperSegments, ct);

            // Step 3: Merge results - assign speaker IDs to transcript segments
            if (diarizationResult.IsSuccess && diarizationResult.Segments.Count > 0)
            {
                AssignSpeakersToSegments(transcript, diarizationResult);
                LoggerService.Info($"Speaker diarization completed: {diarizationResult.SpeakerCount} speakers identified");
            }
        }
        else
        {
            LoggerService.Info("Speaker diarization skipped - model not available");
        }

        progress?.Report(1.0f);
        return transcript;
    }

    private static void AssignSpeakersToSegments(
        Transcript transcript,
        SpeakerDiarizationResult diarizationResult)
    {
        foreach (var segment in transcript.Segments)
        {
            // Find the speaker segment that overlaps most with this transcript segment
            var overlappingSpeaker = diarizationResult.Segments
                .Where(s => s.StartTime < segment.End && s.EndTime > segment.Start)
                .OrderByDescending(s => GetOverlapDuration(s, segment))
                .FirstOrDefault();

            segment.SpeakerId = overlappingSpeaker?.SpeakerId;
        }
    }

    private static TimeSpan GetOverlapDuration(SpeakerSegment speaker, TranscriptSegment transcript)
    {
        var overlapStart = speaker.StartTime > transcript.Start ? speaker.StartTime : transcript.Start;
        var overlapEnd = speaker.EndTime < transcript.End ? speaker.EndTime : transcript.End;
        return overlapEnd > overlapStart ? overlapEnd - overlapStart : TimeSpan.Zero;
    }
}