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
                catch (Exception ex)
                {
                    LoggerService.Warning($"Failed to read existing encryption key, will regenerate: {ex.Message}");
                }
            }

            _key = new byte[32];
            RandomNumberGenerator.Fill(_key);

            try
            {
                var dir = Path.GetDirectoryName(keyPath)!;
                Directory.CreateDirectory(dir);
                SetDirectoryPermissions(dir);
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
            var psi = new System.Diagnostics.ProcessStartInfo("chmod")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("600");
            psi.ArgumentList.Add(path);
            System.Diagnostics.Process.Start(psi)?.WaitForExit(1000);
        }
        catch (Exception ex)
        {
            LoggerService.Debug($"Failed to set file permissions (best effort): {ex.Message}");
        }
    }

    private static void SetDirectoryPermissions(string dirPath)
    {
        if (OperatingSystem.IsWindows())
            return;

        // Unix: chmod 700 (owner rwx only) for the directory
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo("chmod")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("700");
            psi.ArgumentList.Add(dirPath);
            System.Diagnostics.Process.Start(psi)?.WaitForExit(1000);
        }
        catch (Exception ex)
        {
            LoggerService.Debug($"Failed to set directory permissions (best effort): {ex.Message}");
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
            throw new InvalidOperationException("Encryption failed", ex);
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
                throw new InvalidOperationException("Encrypted data is too short or corrupted");

            var nonce = data[..12];
            var tag = data[12..28];
            var cipherBytes = data[28..];

            var plainBytes = new byte[cipherBytes.Length];
            using var aes = new AesGcm(key, 16);
            aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

            return Encoding.UTF8.GetString(plainBytes);
        }
        catch (Exception ex)
        {
            LoggerService.Error("Decryption failed", ex);
            throw new InvalidOperationException("Decryption failed", ex);
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
        catch (Exception ex)
        {
            LoggerService.Debug($"Encryption validation failed: {ex.Message}");
            return false;
        }
    }

    public void ClearEntropyCache()
    {
        lock (_lock)
        {
            if (_key != null)
            {
                CryptographicOperations.ZeroMemory(_key);
                _key = null;
            }
        }
    }
}
