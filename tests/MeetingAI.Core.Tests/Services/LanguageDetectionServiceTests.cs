using FluentAssertions;
using MeetingAI.Core.Services;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class LanguageDetectionServiceTests
{
    private readonly LanguageDetectionService _sut = new();

    [Fact]
    public void Detect_NullText_ReturnsMixed()
    {
        var result = _sut.Detect(null!);
        result.Should().Be(LanguageType.Mixed);
    }

    [Fact]
    public void Detect_EmptyText_ReturnsMixed()
    {
        var result = _sut.Detect("");
        result.Should().Be(LanguageType.Mixed);
    }

    [Fact]
    public void Detect_WhitespaceText_ReturnsMixed()
    {
        var result = _sut.Detect("   ");
        result.Should().Be(LanguageType.Mixed);
    }

    [Fact]
    public void Detect_ChineseText_ReturnsZhCn()
    {
        var result = _sut.Detect("今天天气很好，我们去公园散步。");
        result.Should().Be(LanguageType.ZhCn);
    }

    [Fact]
    public void Detect_EnglishText_ReturnsEnUs()
    {
        var result = _sut.Detect("The quick brown fox jumps over the lazy dog.");
        result.Should().Be(LanguageType.EnUs);
    }

    [Fact]
    public void Detect_MixedText_DetectsDominant_EnUs()
    {
        var result = _sut.Detect("Hello 世界，this is 一个 test");
        result.Should().Be(LanguageType.EnUs);
    }

    [Fact]
    public void Detect_ChineseWithPunctuation_ReturnsZhCn()
    {
        var result = _sut.Detect("【重要通知】会议将于下午3点开始。请准时参加！");
        result.Should().Be(LanguageType.ZhCn);
    }

    [Fact]
    public void Detect_EnglishWithNumbers_ReturnsEnUs()
    {
        var result = _sut.Detect("Version 2.0 is released on 2024-01-15 with 3 new features.");
        result.Should().Be(LanguageType.EnUs);
    }

    [Fact]
    public void Detect_ShortChineseText_ReturnsZhCn()
    {
        var result = _sut.Detect("会议结束");
        result.Should().Be(LanguageType.ZhCn);
    }
}
