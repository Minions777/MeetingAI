namespace MeetingAI.Shared.Constants;

public static class AppConstants
{
    public const string AppName = "MeetingAI";
    public const string AppVersion = "2.0.0";

    public static class Paths
    {
        public static string AppData => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppName);

        public static string Recordings => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            AppName, "Recordings");

        public static string Logs => Path.Combine(AppData, "Logs");
        public static string Settings => Path.Combine(AppData, "settings.json");
        public static string Backup => Path.Combine(AppData, "backup");
    }

    public static class Defaults
    {
        public const int MaxTokens = 4096;
        public const double Temperature = 0.7;
        public const double TopP = 0.9;
        public const int TimeoutSeconds = 120;
        public const string SystemPrompt = "你是一个专业的会议助手，负责总结会议内容、提取关键信息、生成结构化的会议报告。";
    }
}
