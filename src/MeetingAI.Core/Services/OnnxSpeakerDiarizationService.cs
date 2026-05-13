using MeetingAI.Core.Models;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Core.Services;

public sealed class OnnxSpeakerDiarizationService : ISpeakerDiarizationService, IDisposable
{
    private readonly string? _modelPath;
    private bool _disposed;

    public OnnxSpeakerDiarizationService(string? modelPath = null)
    {
        _modelPath = modelPath;
    }

    public bool IsModelAvailable => !string.IsNullOrEmpty(_modelPath) && File.Exists(_modelPath);

    public Task<SpeakerDiarizationResult> ProcessAsync(
        string audioFilePath,
        IReadOnlyList<(TimeSpan Start, TimeSpan End)> whisperSegments,
        CancellationToken ct = default)
    {
        if (!IsModelAvailable)
        {
            return Task.FromResult(new SpeakerDiarizationResult
            {
                IsSuccess = false,
                ErrorMessage = "Speaker diarization model not available"
            });
        }

        LoggerService.Info($"ONNX model found at {_modelPath}, inference not yet implemented");
        return Task.FromResult(new SpeakerDiarizationResult
        {
            IsSuccess = false,
            ErrorMessage = "ONNX model inference not yet implemented"
        });
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