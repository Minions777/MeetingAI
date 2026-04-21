using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MeetingAI.Models;

public enum AIProvider
{
    OpenAI,
    Claude,
    Gemini,
    DeepSeek,
    Ollama
}

public class AIModelConfig : INotifyPropertyChanged
{
    private string _name = "默认配置";
    public string Name
    {
        get => _name;
        set => SetField(ref _name, value);
    }

    private AIProvider _provider = AIProvider.OpenAI;
    public AIProvider Provider
    {
        get => _provider;
        set { _provider = value; OnPropertyChanged(); }
    }

    private string _apiKey = "";
    public string ApiKey
    {
        get => _apiKey;
        set => SetField(ref _apiKey, value);
    }

    private string _baseUrl = "https://api.openai.com/v1";
    public string BaseUrl
    {
        get => _baseUrl;
        set => SetField(ref _baseUrl, value);
    }

    private string _model = "gpt-4o-mini";
    public string Model
    {
        get => _model;
        set => SetField(ref _model, value);
    }

    private double _temperature = 0.7;
    public double Temperature
    {
        get => _temperature;
        set => SetField(ref _temperature, value);
    }

    private int _maxTokens = 2000;
    public int MaxTokens
    {
        get => _maxTokens;
        set => SetField(ref _maxTokens, value);
    }

    public Dictionary<AIProvider, (string DefaultModel, string BaseUrl)> ProviderDefaults => new()
    {
        [AIProvider.OpenAI] = ("gpt-4o-mini", "https://api.openai.com/v1"),
        [AIProvider.Claude] = ("claude-3-5-sonnet-20241022", "https://api.anthropic.com/v1"),
        [AIProvider.Gemini] = ("gemini-2.0-flash-exp", "https://generativelanguage.googleapis.com/v1beta"),
        [AIProvider.DeepSeek] = ("deepseek-chat", "https://api.deepseek.com/v1"),
        [AIProvider.Ollama] = ("llama3.2", "http://localhost:11434/v1")
    };

    public void ApplyProviderDefaults()
    {
        var defaults = ProviderDefaults[Provider];
        Model = defaults.DefaultModel;
        BaseUrl = defaults.BaseUrl;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}