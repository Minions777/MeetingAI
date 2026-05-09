namespace MeetingAI.Core.Models;

public class ChatRequest
{
    public string Model { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
    public double Temperature { get; set; } = 0.7;
    public double TopP { get; set; } = 0.9;
    public int MaxTokens { get; set; } = 4096;
    public bool Stream { get; set; } = false;
}

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Content { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int TokensUsed { get; set; }
    public string FinishReason { get; set; } = string.Empty;
    public bool IsSuccess { get; set; } = true;
}
