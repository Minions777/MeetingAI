using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using MeetingAI.Core.State;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class StreamingAnalysisServiceTests
{
    private readonly MeetingStateManager _stateManager;
    private readonly StreamingAnalysisService _sut;

    public StreamingAnalysisServiceTests()
    {
        _stateManager = new MeetingStateManager();
        _sut = new StreamingAnalysisService(_stateManager);
    }

    [Fact]
    public async Task StreamAnalysisAsync_ProcessesChunks_AccumulatesContent()
    {
        var chunks = new[] { "Hello", " ", "World", "!" };
        var stream = ToAsyncEnumerable(chunks);

        var result = await _sut.StreamAnalysisAsync("meeting-1", AnalysisType.Summary, stream);

        result.IsSuccess.Should().BeTrue();
        result.Content.Should().Be("Hello World!");
        result.MeetingId.Should().Be("meeting-1");
        result.Type.Should().Be(AnalysisType.Summary);
        result.ProcessingDuration.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task StreamAnalysisAsync_ProgressEvent_FiresForEvery10Chunks()
    {
        var chunks = Enumerable.Range(0, 25).Select(i => $"chunk{i}").ToArray();
        var stream = ToAsyncEnumerable(chunks);
        var progressEvents = new List<StreamProgressEventArgs>();

        _sut.ProgressChanged += (_, e) => progressEvents.Add(e);

        await _sut.StreamAnalysisAsync("meeting-2", AnalysisType.Summary, stream);

        progressEvents.Should().HaveCount(2); // chunks 10 and 20
        progressEvents[0].ChunksProcessed.Should().Be(10);
        progressEvents[0].CurrentLength.Should().BeGreaterThan(0);
        progressEvents[1].ChunksProcessed.Should().Be(20);
    }

    
    [Fact]
    public async Task StreamAnalysisAsync_Exception_ReturnsFailedResult()
    {
        var chunks = new[] { "ok", "after-error" };
        var stream = new FailingAsyncEnumerable(chunks);

        var result = await _sut.StreamAnalysisAsync("meeting-4", AnalysisType.Summary, stream);

        result.IsSuccess.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetCurrentContent_ReturnsNull_ForUnknownStreamId()
    {
        var result = _sut.GetCurrentContent("nonexistent-stream-id");
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStreamChunks_ReturnsEmpty_AfterStreamCleanup()
    {
        var chunks = new[] { "a", "b", "c" };
        var stream = ToAsyncEnumerable(chunks);

        await _sut.StreamAnalysisAsync("meeting-6", AnalysisType.Summary, stream);

        var retrieved = _sut.GetStreamChunks("meeting-6");
        retrieved.Should().BeEmpty(); // Cleaned up after stream ends
    }

    [Fact]
    public async Task StreamAnalysisAsync_AddsResultToStateManager()
    {
        var stream = ToAsyncEnumerable(new[] { "final content" });

        var result = await _sut.StreamAnalysisAsync("meeting-7", AnalysisType.Summary, stream);

        result.IsSuccess.Should().BeTrue();
        result.Content.Should().Be("final content");
        result.MeetingId.Should().Be("meeting-7");
    }

    private static async IAsyncEnumerable<string> ToAsyncEnumerable(string[] chunks)
    {
        foreach (var chunk in chunks)
        {
            await Task.Yield();
            yield return chunk;
        }
    }

    private sealed class FailingAsyncEnumerable : IAsyncEnumerable<string>
    {
        private readonly string[] _chunks;
        private bool _thrown;

        public FailingAsyncEnumerable(string[] chunks) => _chunks = chunks;

        public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new FailingEnumerator(_chunks);
        }

        private sealed class FailingEnumerator : IAsyncEnumerator<string>
        {
            private readonly string[] _chunks;
            private int _index;
            private bool _thrown;

            public FailingEnumerator(string[] chunks) => _chunks = chunks;
            public string Current => _chunks[_index - 1];
            public ValueTask<bool> MoveNextAsync()
            {
                if (_index >= _chunks.Length)
                    return ValueTask.FromResult(false);
                if (_index == 1 && !_thrown)
                {
                    _thrown = true;
                    throw new InvalidOperationException("Simulated stream error");
                }
                _index++;
                return ValueTask.FromResult(_index <= _chunks.Length);
            }
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}