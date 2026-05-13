using FluentAssertions;
using MeetingAI.Core.Services;
using MeetingAI.Shared.Configuration;
using Moq;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

[Collection("NonParallel")]
public class RecordingServiceTests
{
    private readonly Mock<IAudioCapture> _audioCaptureMock;
    private readonly RecordingService _sut;
    private readonly RecordingOptions _testOptions;
    private bool _isRecording;

    public RecordingServiceTests()
    {
        _isRecording = false;
        _audioCaptureMock = new Mock<IAudioCapture>();
        _audioCaptureMock.Setup(x => x.SampleRate).Returns(44100);
        _audioCaptureMock.Setup(x => x.Channels).Returns(2);
        _audioCaptureMock.Setup(x => x.IsRecording).Returns(() => _isRecording);
        _audioCaptureMock.Setup(x => x.StartRecording()).Callback(() => _isRecording = true);
        _audioCaptureMock.Setup(x => x.StopRecording()).Callback(() => _isRecording = false);
        _sut = new RecordingService(_audioCaptureMock.Object);
        _testOptions = new RecordingOptions
        {
            OutputDirectory = Path.Combine(Path.GetTempPath(), $"MeetingAI_Test_{Guid.NewGuid():N}")
        };
    }

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var service = new RecordingService(_audioCaptureMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public void IsRecording_Initially_False()
    {
        _sut.IsRecording.Should().BeFalse();
    }

    [Fact]
    public void IsPaused_Initially_False()
    {
        _sut.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void Duration_WhenNotRecording_ReturnsZero()
    {
        _sut.Duration.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task StartRecordingAsync_WhenAlreadyRecording_ThrowsInvalidOperationException()
    {
        _audioCaptureMock.Setup(x => x.IsRecording).Returns(true);
        await _sut.Invoking(s => s.StartRecordingAsync())
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StartRecordingAsync_CreatesOutputDirectory()
    {
        await _sut.StartRecordingAsync(_testOptions);
        _audioCaptureMock.Verify(x => x.StartRecording(), Times.Once);
    }

    [Fact]
    public async Task StopRecordingAsync_WhenNotRecording_ThrowsInvalidOperationException()
    {
        await _sut.Invoking(s => s.StopRecordingAsync())
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task StopRecordingAsync_StopsAudioCapture()
    {
        await _sut.StartRecordingAsync(_testOptions);
        var result = await _sut.StopRecordingAsync();

        _audioCaptureMock.Verify(x => x.StopRecording(), Times.Once);
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PauseResume_WorksCorrectly()
    {
        await _sut.StartRecordingAsync(_testOptions);

        _sut.Pause();
        _sut.IsPaused.Should().BeTrue();

        _sut.Resume();
        _sut.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void Pause_WhenNotRecording_DoesNothing()
    {
        _sut.Pause();
        _sut.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void Resume_WhenNotPaused_DoesNothing()
    {
        _sut.Resume();
        _sut.IsPaused.Should().BeFalse();
    }

    [Fact]
    public void Dispose_CleansUp()
    {
        _sut.Dispose();
        _audioCaptureMock.Verify(x => x.Dispose(), Times.Once);
    }
}
