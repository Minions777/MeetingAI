using System.Security.Cryptography;
using System.Text;
using MeetingAI.Shared.Logging;

namespace MeetingAI.Shared.Configuration;

/// <summary>
/// Cross-platform secure storage using AES-256-GCM.
/// Key is stored in a file with restricted permissions.
/// </summary>
public class AesSecureStorage : ISecureStorage
{
    private byte[]? _key;
    private readonly object _lock = new();

    private static string KeyPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MeetingAI", ".key");

    private byte[] GetOrCreateKey()
    {
        if (_key != null)
            return _key;

        lock (_lock)
        {
            if (_key != null)
                return _key;

            var keyPath = KeyPath;
            if (File.Exists(keyPath))
            {
                try
                {
                    _key = File.ReadAllBytes(keyPath);
                    if (_key.Length == 32)
                        return _key;
                }
                catch
                {
                    // Regenerate if read fails
                }
            }

            _key = new byte[32];
            RandomNumberGenerator.Fill(_key);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(keyPath)!);
                File.WriteAllBytes(keyPath, _key);
                SetFilePermissions(keyPath);
            }
            catch (Exception ex)
            {
                LoggerService.Warning($"Failed to save encryption key: {ex.Message}");
            }

            return _key;
        }
    }

    private static void SetFilePermissions(string path)
    {
        if (OperatingSystem.IsWindows())
            return; // Windows file ACLs are sufficient by default for user profile

        // Unix: chmod 600 (owner read/write only)
        try
        {
            System.Diagnostics.Process.Start("chmod", $"600 \"{path}\"")?.WaitForExit(1000);
        }
        catch
        {
            // Best effort
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return string.Empty;

        try
        {
            var key = GetOrCreateKey();
            var nonce = new byte[12]; // AES-GCM nonce
            RandomNumberGenerator.Fill(nonce);

            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = new byte[plainBytes.Length];
            var tag = new byte[16];

            using var aes = new AesGcm(key, 16);
            aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

            // Format: base64(nonce + tag + ciphertext)
            var result = new byte[nonce.Length + tag.Length + cipherBytes.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(tag, 0, result, nonce.Length, tag.Length);
            Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length + tag.Length, cipherBytes.Length);

            return Convert.ToBase64String(result);
        }
        catch (Exception ex)
        {
            LoggerService.Error("Encryption failed", ex);
            return plainText;
        }
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return string.Empty;

        try
        {
            var key = GetOrCreateKey();
            var data = Convert.FromBase64String(encryptedText);

            if (data.Length < 28) // 12 nonce + 16 tag minimum
                return encryptedText;

            var nonce = data[..12];
            var tag = data[12..28];
            var cipherBytes = data[28..];

            var plainBytes = new byte[cipherBytes.Length];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch
        {
            // If decryption fails, return as-is (might be plain text or different format)
            return encryptedText;
        }
    }

    public void EncryptConfig(ProviderConfig config)
    {
        config.ApiKey = Encrypt(config.ApiKey);
        config.UpdatedAt = DateTime.UtcNow;
    }

    public void DecryptConfig(ProviderConfig config)
    {
        config.ApiKey = Decrypt(config.ApiKey);
    }

    public bool ValidateEncryption(string originalText)
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

    public void ClearEntropyCache()
    {
        lock (_lock)
        {
            _key = null;
        }
    }
}
