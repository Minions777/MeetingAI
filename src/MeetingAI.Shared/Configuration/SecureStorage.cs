using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MeetingAI.Shared.Configuration;

/// <summary>
/// 使用 Windows DPAPI 加密敏感配置
/// </summary>
public static class SecureStorage
{
    private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("MeetingAI_v2_Entropy_2024");
    
    public static string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;
            
        try
        {
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = ProtectedData.Protect(
                plainBytes, 
                Entropy, 
                DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(encryptedBytes);
        }
        catch
        {
            return plainText; // Fallback to plain text
        }
    }
    
    public static string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;
            
        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedText);
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes, 
                Entropy, 
                DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
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
}
