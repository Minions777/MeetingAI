namespace MeetingAI.Shared.Configuration;

public interface IConfigurationService
{
    AppSettings Load();
    Task<AppSettings> LoadAsync();
    void Save(AppSettings settings);
    Task SaveAsync(AppSettings settings);
    AppSettings Reload();
    void ClearCache();
    string GetBackupPath();
    void Backup();
    bool RestoreFromBackup(string backupPath);
    IEnumerable<FileInfo> GetBackupFiles();
    (bool IsValid, List<string> Errors) ValidateConfiguration();
    string ExportSafe();

    event EventHandler? SettingsChanged;
}
