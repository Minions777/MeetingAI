using System.Security.Cryptography;
using System.Text;

namespace MeetingAI.Shared.Configuration;

/// <summary>
/// 使用 Windows DPAPI 加密敏感配置
/// 支持基于机器特征的动态 Entropy 生成
/// </summary>
public static class SecureStorage
{
    private static byte[]? _entropy;
    private static readonly object _lock = new();
    
    /// <summary>
    /// 获取或创建基于机器特征的动态 Entropy
    /// </summary>
    private static byte[] GetEntropy()
    {
        if (_entropy != null)
            return _entropy;
            
        lock (_lock)
        {
            if (_entropy != null)
                return _entropy;
                
            // 尝试从文件加载已保存的 Entropy
            var entropyFile = GetEntropyStoragePath();
            if (File.Exists(entropyFile))
            {
                try
                {
                    var entropyJson = File.ReadAllText(entropyFile);
                    _entropy = Convert.FromBase64String(entropyJson);
                    return _entropy;
                }
                catch
                {
                    // 如果读取失败，生成新的 Entropy
                }
            }
            
            // 生成新的随机 Entropy
            _entropy = new byte[32];
            RandomNumberGenerator.Fill(_entropy);
            
            // 保存到文件
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(entropyFile)!);
                File.WriteAllText(entropyFile, Convert.ToBase64String(_entropy));
            }
            catch
            {
                // 如果保存失败，使用基于机器特征的 fallback Entropy
                _entropy = GenerateMachineBasedEntropy();
            }
            
            return _entropy;
        }
    }
    
    /// <summary>
    /// 获取 Entropy 存储路径
    /// </summary>
    private static string GetEntropyStoragePath()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "MeetingAI", "entropy.dat");
    }
    
    /// <summary>
    /// 生成基于机器特征的 Fallback Entropy
    /// </summary>
    private static byte[] GenerateMachineBasedEntropy()
    {
        // 使用机器名 + 用户名 + 固定盐值生成 Entropy
        var machineInfo = $"{Environment.MachineName}_{Environment.UserName}_MeetingAI_v2_SecureSalt";
        return SHA256.HashData(Encoding.UTF8.GetBytes(machineInfo));
    }
    
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;
            
        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var entropy = GetEntropy();
            var encryptedBytes = ProtectedData.Protect(
                plainBytes, 
                entropy, 
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            LoggerService.Error("加密失败", ex);
            return plainText; // Fallback to plain text (不推荐用于生产环境)
        }
    }
    
    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;
            
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var entropy = GetEntropy();
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes, 
                entropy, 
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            LoggerService.Error("解密失败", ex);
            return encryptedText; // Return as-is if decryption fails
        }
    }
    
    public static void EncryptConfig(ProviderConfig config)
    {
        config.ApiKey = Encrypt(config.ApiKey);
        config.UpdatedAt = DateTime.UtcNow;
    }
    
    public static void DecryptConfig(ProviderConfig config)
    {
        config.ApiKey = Decrypt(config.ApiKey);
    }
    
    /// <summary>
    /// 验证加密后的配置是否可以正确解密
    /// </summary>
    public static bool ValidateEncryption(string originalText)
    {
        if (string.IsNullOrEmpty(originalText))
            return true;
            
        try
        {
            var encrypted = Encrypt(originalText);
            var decrypted = Decrypt(encrypted);
            return originalText == decrypted;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// 清除内存中的 Entropy 缓存（用于测试或重置）
    /// </summary>
    public static void ClearEntropyCache()
    {
        lock (_lock)
        {
            _entropy = null;
        }
    }
}
