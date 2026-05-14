using FluentAssertions;
using MeetingAI.Core.Services;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class OnnxSpeakerDiarizationServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _tempModelPath;

    public OnnxSpeakerDiarizationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "MeetingAI.Tests", Guid.NewGuid().ToString("N"));
        _tempModelPath = Path.Combine(_tempDir, "model.onnx");
    }

    [Fact]
    public void IsModelAvailable_NullModelPath_ReturnsFalse()
    {
        using var sut = new OnnxSpeakerDiarizationService(null);
        sut.IsModelAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsModelAvailable_EmptyModelPath_ReturnsFalse()
    {
        using var sut = new OnnxSpeakerDiarizationService("");
        sut.IsModelAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsModelAvailable_NonExistentPath_ReturnsFalse()
    {
        using var sut = new OnnxSpeakerDiarizationService("/nonexistent/model.onnx");
        sut.IsModelAvailable.Should().BeFalse();
    }

    [Fact]
    public void IsModelAvailable_ExistingFile_ReturnsTrue()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_tempModelPath, "dummy-model-data");

        using var sut = new OnnxSpeakerDiarizationService(_tempModelPath);
        sut.IsModelAvailable.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessAsync_ModelNotAvailable_ReturnsFailure()
    {
        using var sut = new OnnxSpeakerDiarizationService(null);
        var segments = new List<(TimeSpan, TimeSpan)> { (TimeSpan.Zero, TimeSpan.FromSeconds(1)) };

        var result = await sut.ProcessAsync("test.wav", segments);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("model not available");
    }

    [Fact]
    public async Task ProcessAsync_ModelAvailable_ReturnsNotImplementedYet()
    {
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(_tempModelPath, "dummy-model-data");

        using var sut = new OnnxSpeakerDiarizationService(_tempModelPath);
        var segments = new List<(TimeSpan, TimeSpan)> { (TimeSpan.Zero, TimeSpan.FromSeconds(1)) };

        var result = await sut.ProcessAsync("test.wav", segments);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not yet implemented");
    }

    [Fact]
    public async Task ProcessAsync_EmptySegments_DoesNotThrow()
    {
        using var sut = new OnnxSpeakerDiarizationService(null);
        var emptySegments = new List<(TimeSpan, TimeSpan)>();

        var act = () => sut.ProcessAsync("test.wav", emptySegments);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void Dispose_MultipleCalls_DoesNotThrow()
    {
        var sut = new OnnxSpeakerDiarizationService(null);
        sut.Dispose();
        sut.Dispose();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, recursive: true); }
            catch { }
        }
    }
}
