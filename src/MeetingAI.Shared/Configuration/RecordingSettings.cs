namespace MeetingAI.Shared.Configuration;

public class RecordingSettings
{
    public string OutputDirectory { get; set; } = string.Empty;
    public bool IncludeMicrophone { get; set; } = true;
    public bool IncludeSystemAudio { get; set; } = true;
    public AudioQuality Quality { get; set; } = AudioQuality.High;
    public string AudioFormat { get; set; } = "wav";
}

public enum AudioQuality
{
    Low = 16000,
    Medium = 22050,
    High = 44100
}
