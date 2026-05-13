namespace MeetingAI.Core.Constants;

public static class AIPromptTemplates
{
    public const string AskAIPrompt = @"基于以下会议片段回答问题。
选中文本：{0}
当前讨论：{1}
时间戳：{2}

要求：
1. 回答 ≤100 字
2. 引用相关时间戳 [mm:ss]
3. 只回答事实，不臆测
4. 如果问题与上下文无关，回复""请选中相关会议文本后再次提问""";

    public static string BuildAskAIPrompt(string selectedText, string context, TimeSpan? timestamp = null)
    {
        var timestampStr = timestamp.HasValue
            ? timestamp.Value.ToString(@"mm\:ss")
            : "未知";

        return string.Format(
            AskAIPrompt,
            selectedText,
            context,
            timestampStr);
    }
}