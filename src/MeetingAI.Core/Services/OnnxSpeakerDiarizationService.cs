using MeetingAI.Core.Models;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

/// <summary>
/// ONNX-based speaker diarization implementation.
/// This is a stub implementation that logs when the model is unavailable.
/// </summary>
public sealed class OnnxSpeakerDiarizationService : ISpeakerDiarizationService, IDisposable
{
    private readonly string? _modelPath;
    private bool _disposed;

    public OnnxSpeakerDiarizationService(string? modelPath = null)
    {
        _modelPath = modelPath;
    }

    public bool IsModelAvailable => !string.IsNullOrEmpty(_modelPath) && File.Exists(_modelPath);

    public async Task<SpeakerDiarizationResult> ProcessAsync(
        string audioFilePath,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> whisperSegments,
        CancellationToken ct = default)
    {
        if (!IsModelAvailable)
        {
            LoggerService.Info("Speaker diarization model not available, skipping");
            return new SpeakerDiarizationResult
            {
                IsSuccess = false,
                ErrorMessage = "Speaker diarization model not available"
            };
        }

        // TODO: When ONNX model is provided:
        // 1. Load audio file and prepare features (mel-spectrogram or waveform)
        // 2. Run inference through ONNX Runtime
        // 3. Apply clustering (K-means or spectral clustering) for 2-8 speakers
        // 4. Return aligned SpeakerSegments

        LoggerService.Info($"[STUB] ONNX speaker diarization for {audioFilePath} with {whisperSegments.Count} segments");

        await Task.Delay(1, ct); // Placeholder for actual processing

        return new SpeakerDiarizationResult
        {
            IsSuccess = false,
            ErrorMessage = "ONNX model integration pending"
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}