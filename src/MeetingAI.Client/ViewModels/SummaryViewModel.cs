using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MeetingAI.Core.Models;
using MeetingAI.Core.Services;
using MeetingAI.Shared.Configuration;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Client.ViewModels;

public partial class SummaryViewModel : ObservableObject
{
    private readonly ISummaryService _summaryService;
    private readonly IConfigurationService _configService;
    private readonly ITranscriptionService _transcriptionService;
    private readonly IAIAssistantService _aiAssistantService;

    [ObservableProperty] private string _transcriptText = "";
    [ObservableProperty] private string _summaryText = "";
    [ObservableProperty] private bool _hasSummary;
    [ObservableProperty] private bool _isStreaming;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private string _statusText = "";

    // Ask AI feature
    [ObservableProperty] private string _selectedText = "";
    [ObservableProperty] private string _aiResponseText = "";
    [ObservableProperty] private bool _isAskingAI;
    [ObservableProperty] private bool _showAIPanel;
    [ObservableProperty] private string _aiErrorMessage = "";

    public SummaryViewModel(
        ISummaryService summaryService,
        IConfigurationService configService,
        ITranscriptionService transcriptionService,
        IAIAssistantService aiAssistantService)
    {
        _summaryService = summaryService;
        _configService = configService;
        _transcriptionService = transcriptionService;
        _aiAssistantService = aiAssistantService;
    }

    public async Task<Transcript?> TranscribeAsync(string audioFilePath, string? providerId)
    {
        IsProcessing = true;
        StatusText = "转录中...";

        try
        {
            var transcript = await _transcriptionService.TranscribeAsync(audioFilePath, providerId);
            TranscriptText = transcript.Text;
            StatusText = "成功";
            LoggerService.Info("Transcription completed");
            return transcript;
        }
        catch (Exception ex)
        {
            LoggerService.Error("Transcription failed", ex);
            StatusText = $"错误: {ex.Message}";
            return null;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public async Task<Summary?> SummarizeAsync(Transcript transcript, string? providerId)
    {
        IsProcessing = true;
        StatusText = "生成摘要中...";

        try
        {
            var summary = await _summaryService.SummarizeAsync(transcript, providerId);
            SummaryText = FormatSummary(summary);
            HasSummary = !string.IsNullOrWhiteSpace(SummaryText);
            StatusText = "成功";
            LoggerService.Info("Summary generated");
            return summary;
        }
        catch (Exception ex)
        {
            LoggerService.Error("Summarization failed", ex);
            StatusText = $"错误: {ex.Message}";
            return null;
        }
        finally
        {
            IsProcessing = false;
        }
    }

    public async Task StreamSummarizeAsync(Transcript transcript)
    {
        IsProcessing = true;
        IsStreaming = true;
        SummaryText = string.Empty;
        StatusText = "生成摘要中...";

        try
        {
            var sb = new System.Text.StringBuilder();
            await foreach (var chunk in _summaryService.StreamSummarizeAsync(transcript))
            {
                sb.Append(chunk);
                var text = sb.ToString();
                Dispatcher.UIThread.Post(() => SummaryText = text);
            }

            var finalText = sb.ToString();
            Dispatcher.UIThread.Post(() =>
            {
                SummaryText = finalText;
                HasSummary = !string.IsNullOrWhiteSpace(finalText);
            });
            StatusText = "成功";
            LoggerService.Info("Streaming summary completed");
        }
        catch (Exception ex)
        {
            LoggerService.Error("Streaming summarization failed", ex);
            StatusText = $"错误: {ex.Message}";
        }
        finally
        {
            IsStreaming = false;
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task AskAIAsync(string? selectedText)
    {
        if (string.IsNullOrWhiteSpace(selectedText))
        {
            AiErrorMessage = "请先选中相关会议文本";
            return;
        }

        SelectedText = selectedText;
        AiResponseText = string.Empty;
        IsAskingAI = true;
        ShowAIPanel = true;
        AiErrorMessage = string.Empty;

        try
        {
            var context = GetRecentContext();
            var sb = new System.Text.StringBuilder();

            await foreach (var chunk in _aiAssistantService.AskAsync(selectedText, context, null))
            {
                sb.Append(chunk);
                var text = sb.ToString();
                Dispatcher.UIThread.Post(() => AiResponseText = text);
            }

            LoggerService.Info("AI Assistant response completed");
        }
        catch (Exception ex)
        {
            LoggerService.Error("AI Assistant failed", ex);
            AiErrorMessage = $"错误: {ex.Message}";
        }
        finally
        {
            IsAskingAI = false;
        }
    }

    [RelayCommand]
    private void CloseAIPanel()
    {
        ShowAIPanel = false;
        AiResponseText = string.Empty;
        AiErrorMessage = string.Empty;
    }

    private string GetRecentContext()
    {
        // Get recent transcript context (last 60 seconds worth)
        var text = TranscriptText;
        if (string.IsNullOrEmpty(text))
            return "（无完整转录内容）";

        // Return the full transcript as context since we don't have fine-grained timestamps
        return text.Length > 2000 ? text[..2000] + "..." : text;
    }

    public void Clear()
    {
        TranscriptText = string.Empty;
        SummaryText = string.Empty;
        HasSummary = false;
        ShowAIPanel = false;
        AiResponseText = string.Empty;
        AiErrorMessage = string.Empty;
    }

    public string FormatSummary(Summary? summary)
    {
        if (summary == null) return string.Empty;

        var sb = new System.Text.StringBuilder();

        if (!string.IsNullOrEmpty(summary.Overview))
            sb.AppendLine($"**会议概要**: {summary.Overview}");

        if (summary.KeyPoints.Any())
        {
            sb.AppendLine("\n**关键要点**:");
            foreach (var point in summary.KeyPoints)
                sb.AppendLine($"  • {point}");
        }

        if (summary.ActionItems.Any())
        {
            sb.AppendLine("\n**行动项**:");
            foreach (var item in summary.ActionItems)
                sb.AppendLine($"  • {item.Description}");
        }

        if (summary.Decisions.Any())
        {
            sb.AppendLine("\n**决议**:");
            foreach (var decision in summary.Decisions)
                sb.AppendLine($"  • {decision}");
        }

        return sb.ToString();
    }
}