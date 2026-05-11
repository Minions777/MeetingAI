namespace MeetingAI.Core.Models;

public class AudioData
{
    public byte[] Bytes { get; set; } = Array.Empty<byte>();
    public string? FilePath { get; set; }
    public long Length { get; set; }
    public string Format { get; set; } = "wav";
    public int SampleRate { get; set; } = 44100;
    public int Channels { get; set; } = 2;
    public TimeSpan Duration { get; set; }
}

public class TranscriptionOptions
{
    public string Language { get; set; } = "zh";
    public bool EnableTimestamps { get; set; } = true;
    public string? Prompt { get; set; }
}
