using FluentAssertions;
using MeetingAI.Core.Providers;
using MeetingAI.Core.Services;
using MeetingAI.Core.Tests.Helpers;
using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class TranslationServiceTests
{
    private readonly IConfigurationService _configService;
    private readonly ILanguageDetectionService _languageDetection;
    private readonly ProviderManager _providerManager;

    public TranslationServiceTests()
    {
        _configService = TestHelpers.CreateMockConfigService();
        _languageDetection = new LanguageDetectionService();
        _providerManager = new ProviderManager(_configService);
    }

    [Fact]
    public void Constructor_WithValidConfig_DoesNotThrow()
    {
        var sut = new TranslationService(_configService, _languageDetection, _providerManager);
        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task TranslateAsync_EmptyText_ReturnsEmptyResult()
    {
        var sut = new TranslationService(_configService, _languageDetection, _providerManager);
        var result = await sut.TranslateAsync("", LanguageType.ZhCn, "");
        result.Original.Should().BeEmpty();
        result.Translation.Should().BeEmpty();
    }

    [Fact]
    public async Task TranslateAsync_WhitespaceText_ReturnsEmptyResult()
    {
        var sut = new TranslationService(_configService, _languageDetection, _providerManager);
        var result = await sut.TranslateAsync("   ", LanguageType.ZhCn, "");
        result.Original.Should().Be("   ");
        result.Translation.Should().BeEmpty();
    }

    [Fact]
    public async Task TranslateAsync_NoProvidersAvailable_ThrowsInvalidOperationException()
    {
        var emptyConfig = TestHelpers.CreateMockConfigService(new AppSettings());
        var emptyProviderManager = new ProviderManager(emptyConfig);
        var sut = new TranslationService(emptyConfig, _languageDetection, emptyProviderManager);

        await sut.Invoking(s => s.TranslateAsync("test", LanguageType.EnUs, ""))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}
