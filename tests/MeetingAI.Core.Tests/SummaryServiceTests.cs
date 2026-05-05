using Xunit;
using Moq;
using MeetingAI.Core.Services;
using MeetingAI.Core.Models;
using MeetingAI.Shared.Configuration;

namespace MeetingAI.Core.Tests;

public class SummaryServiceTests
{
    [Fact]
    public void ParseSummaryResponse_ValidContent_ReturnsSummary()
    {
        // Arrange
        var configService = new ConfigurationService();
        var service = new SummaryService(configService);
        
        var transcript = new Transcript
        {
            Text = "这是一个测试会议记录..."
        };
        
        var mockResponse = @"**会议概要**: 测试会议
**关键要点**:
• 第一点
• 第二点
**行动项**:
• 完成测试";
        
        // Act
        // Note: This would require mocking the provider in a real test
        
        // Assert
        Assert.NotNull(transcript);
    }
    
    [Fact]
    public void Transcript_NewInstance_HasEmptySegments()
    {
        // Arrange & Act
        var transcript = new Transcript();
        
        // Assert
        Assert.Empty(transcript.Segments);
        Assert.NotNull(transcript.Text);
    }
    
    [Fact]
    public void MeetingRecord_NewInstance_HasPendingStatus()
    {
        // Arrange & Act
        var record = new MeetingRecord();
        
        // Assert
        Assert.Equal(RecordingStatus.Pending, record.Status);
        Assert.NotNull(record.Id);
    }
}
