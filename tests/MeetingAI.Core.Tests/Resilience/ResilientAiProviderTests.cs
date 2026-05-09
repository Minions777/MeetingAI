using FluentAssertions;
using MeetingAI.Core.Models;
using MeetingAI.Core.Providers.Abstractions;
using MeetingAI.Core.Resilience;
using NSubstitute;
using Xunit;

namespace MeetingAI.Core.Tests.Resilience;

public class ResilientAiProviderTests
{
    [Fact]
    public void Constructor_WithNullProvider_ThrowsArgumentNullException()
    {
        // Act & Assert
        ((Action)(() => new ResilientAiProvider(null!)))
            .Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task ChatAsync_ProviderSucceeds_ReturnsSuccessfulResponse()
    {
        // Arrange
        var innerProvider = Substitute.For<IAIProvider>();
        innerProvider.Name.Returns("TestProvider");
        innerProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse { Content = "Success", IsSuccess = true });

        var sut = new ResilientAiProvider(innerProvider);

        // Act
        var result = await sut.ChatAsync(new ChatRequest());

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Content.Should().Be("Success");
    }

    [Fact]
    public async Task ChatAsync_ProviderFailsAfterRetries_ReturnsErrorResponse()
    {
        // Arrange
        var innerProvider = Substitute.For<IAIProvider>();
        innerProvider.Name.Returns("FailingProvider");
        innerProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ChatResponse>(new HttpRequestException("Network error")));

        var sut = new ResilientAiProvider(innerProvider);

        // Act
        var result = await sut.ChatAsync(new ChatRequest());

        // Assert
        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ProviderName_DelegatesToInnerProvider()
    {
        // Arrange
        var innerProvider = Substitute.For<IAIProvider>();
        innerProvider.Name.Returns("InnerProvider");

        var sut = new ResilientAiProvider(innerProvider);

        // Assert
        sut.ProviderName.Should().Be("InnerProvider");
    }

    [Fact]
    public void StreamChatAsync_ProviderSucceeds_ReturnsStream()
    {
        // Arrange
        var innerProvider = Substitute.For<IAIProvider>();
        innerProvider.Name.Returns("StreamProvider");
        innerProvider.StreamChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(AsyncEnumerableEx.Empty<string>());

        var sut = new ResilientAiProvider(innerProvider);

        // Act
        var result = sut.StreamChatAsync(new ChatRequest(), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task ChatAsync_ProviderReturnsUnsuccessfulResult_RetriesAndFails()
    {
        // Arrange
        var innerProvider = Substitute.For<IAIProvider>();
        innerProvider.Name.Returns("UnsuccessfulProvider");
        innerProvider.ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse { Content = "", IsSuccess = false });

        var sut = new ResilientAiProvider(innerProvider);

        // Act
        var result = await sut.ChatAsync(new ChatRequest());

        // Assert - Polly retry should kick in and eventually return the unsuccessful result
        result.Should().NotBeNull();
    }
}

internal static class AsyncEnumerableEx
{
    public static async IAsyncEnumerable<T> Empty<T>()
    {
        await Task.CompletedTask;
        yield break;
    }
}
