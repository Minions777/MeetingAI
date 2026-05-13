namespace MeetingAI.Core.Models;

/// <summary>
/// Represents a single speaker segment in a diarization result.
/// </summary>
/// <param name="SpeakerId">Unique speaker identifier (e.g., "SPEAKER_00").</param>
/// <param name="StartTime">Segment start time.</param>
/// <param name="EndTime">Segment end time.</param>
/// <param name="Confidence">Confidence score between 0 and 1.</param>
public sealed record SpeakerSegment(
    string SpeakerId,
    TimeSpan StartTime,
    TimeSpan EndTime,
    double Confidence);

/// <summary>
/// Contains speaker diarization results for an audio file.
/// </summary>
public sealed class SpeakerDiarizationResult
{
    /// <summary>
    /// List of speaker segments ordered by start time.
    /// </summary>
    public List<SpeakerSegment> Segments { get; set; } = new();

    /// <summary>
    /// Total number of unique speakers detected.
    /// </summary>
    public int SpeakerCount => Segments.Select(s => s.SpeakerId).Distinct().Count();

    /// <summary>
    /// Whether diarization was successful (model available and processed).
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if diarization failed.
    /// </summary>
    public string? ErrorMessage { get; set; }
}