using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using Moq;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class CombinedTranscriptionServiceTests
{
    private readonly Mock<ITranscriptionService> _transcriptionMock;
    private readonly Mock<ISpeakerDiarizationService> _diarizationMock;
    private readonly CombinedTranscriptionService _sut;

    public CombinedTranscriptionServiceTests()
    {
        _transcriptionMock = new Mock<ITranscriptionService>();
        _diarizationMock = new Mock<ISpeakerDiarizationService>();
        _sut = new CombinedTranscriptionService(_transcriptionMock.Object, _diarizationMock.Object);
    }

    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var service = new CombinedTranscriptionService(_transcriptionMock.Object, _diarizationMock.Object);
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task TranscribeWithSpeakerDiarizationAsync_DiarizationAvailable_AssignsSpeakers()
    {
        var transcript = new Transcript
        {
            Text = "Hello World",
            Segments =
            {
                new TranscriptSegment { Id = 1, Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(2), Text = "Hello" },
                new TranscriptSegment { Id = 2, Start = TimeSpan.FromSeconds(2), End = TimeSpan.FromSeconds(4), Text = "World" }
            }
        };

        _transcriptionMock
            .Setup(x => x.TranscribeAsync(It.IsAny<string>(), null, null, null, default))
            .ReturnsAsync(transcript);

        _diarizationMock.Setup(x => x.IsModelAvailable).Returns(true);
        _diarizationMock
            .Setup(x => x.ProcessAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<(TimeSpan, TimeSpan)>>(), default))
            .ReturnsAsync(new SpeakerDiarizationResult
            {
                IsSuccess = true,
                Segments = new List<SpeakerSegment>
                {
                    new("SPEAKER_00", TimeSpan.Zero, TimeSpan.FromSeconds(2), 0.9),
                    new("SPEAKER_01", TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), 0.85)
                }
            });

        var result = await _sut.TranscribeWithSpeakerDiarizationAsync("test.wav");

        result.Should().NotBeNull();
        result.Segments[0].SpeakerId.Should().Be("SPEAKER_00");
        result.Segments[1].SpeakerId.Should().Be("SPEAKER_01");
    }

    [Fact]
    public async Task TranscribeWithSpeakerDiarizationAsync_DiarizationNotAvailable_SkipsAssignment()
    {
        var transcript = new Transcript
        {
            Text = "Test",
            Segments =
            {
                new TranscriptSegment { Id = 1, Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(1), Text = "Test" }
            }
        };

        _transcriptionMock
            .Setup(x => x.TranscribeAsync(It.IsAny<string>(), null, null, null, default))
            .ReturnsAsync(transcript);

        _diarizationMock.Setup(x => x.IsModelAvailable).Returns(false);

        var result = await _sut.TranscribeWithSpeakerDiarizationAsync("test.wav");

        result.Segments[0].SpeakerId.Should().BeNull();
    }

    [Fact]
    public async Task TranscribeWithSpeakerDiarizationAsync_DiarizationFails_ReturnsTranscriptWithoutSpeakers()
    {
        var transcript = new Transcript
        {
            Text = "Test",
            Segments =
            {
                new TranscriptSegment { Id = 1, Start = TimeSpan.Zero, End = TimeSpan.FromSeconds(1), Text = "Test" }
            }
        };

        _transcriptionMock
            .Setup(x => x.TranscribeAsync(It.IsAny<string>(), null, null, null, default))
            .ReturnsAsync(transcript);

        _diarizationMock.Setup(x => x.IsModelAvailable).Returns(true);
        _diarizationMock
            .Setup(x => x.ProcessAsync(It.IsAny<string>(), It.IsAny<IReadOnlyList<(TimeSpan, TimeSpan)>>(), default))
            .ReturnsAsync(new SpeakerDiarizationResult { IsSuccess = false });

        var result = await _sut.TranscribeWithSpeakerDiarizationAsync("test.wav");

        result.Segments[0].SpeakerId.Should().BeNull();
    }
}
