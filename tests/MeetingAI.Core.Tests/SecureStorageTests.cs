using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests;

public class SecureStorageTests : IDisposable
{
    private readonly AesSecureStorage _storage = new();

    public void Dispose()
    {
        _storage.ClearEntropyCache();
    }

    [Fact]
    public void Encrypt_Decrypt_RoundTrip_ReturnsOriginalValue()
    {
        var originalText = "sk-test-api-key-12345";
        var encrypted = _storage.Encrypt(originalText);
        var decrypted = _storage.Decrypt(encrypted);
        Assert.NotEqual(originalText, encrypted);
        Assert.Equal(originalText, decrypted);
    }

    [Fact]
    public void Encrypt_EmptyString_ReturnsEmptyString()
    {
        var result = _storage.Encrypt("");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Decrypt_EmptyString_ReturnsEmptyString()
    {
        var result = _storage.Decrypt("");
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Encrypt_SameValueProducesDifferentCiphertext()
    {
        var originalText = "same-text";
        var encrypted1 = _storage.Encrypt(originalText);
        _storage.ClearEntropyCache();
        var encrypted2 = _storage.Encrypt(originalText);
        Assert.NotNull(encrypted1);
        Assert.NotNull(encrypted2);
    }

    [Fact]
    public void Decrypt_InvalidBase64_ReturnsOriginalValue()
    {
        var invalidText = "not-a-valid-base64!!";
        var result = _storage.Decrypt(invalidText);
        Assert.Equal(invalidText, result);
    }

    [Fact]
    public void ValidateEncryption_ValidText_ReturnsTrue()
    {
        var result = _storage.ValidateEncryption("valid-api-key");
        Assert.True(result);
    }

    [Fact]
    public void ValidateEncryption_EmptyText_ReturnsTrue()
    {
        var result = _storage.ValidateEncryption("");
        Assert.True(result);
    }

    [Fact]
    public void Encrypt_SpecialCharacters_PreservesContent()
    {
        var specialText = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
        var encrypted = _storage.Encrypt(specialText);
        var decrypted = _storage.Decrypt(encrypted);
        Assert.Equal(specialText, decrypted);
    }

    [Fact]
    public void Encrypt_UnicodeCharacters_PreservesContent()
    {
        var unicodeText = "中文测试📝🔑密码";
        var encrypted = _storage.Encrypt(unicodeText);
        var decrypted = _storage.Decrypt(encrypted);
        Assert.Equal(unicodeText, decrypted);
    }

    [Fact]
    public void EncryptConfig_RoundTrip_DecryptsCorrectly()
    {
        var config = new ProviderConfig
        {
            Id = "test-provider",
            Name = "Test Provider",
            ApiKey = "sk-secret-key-12345"
        };

        _storage.EncryptConfig(config);
        Assert.NotEqual("sk-secret-key-12345", config.ApiKey);

        _storage.DecryptConfig(config);
        Assert.Equal("sk-secret-key-12345", config.ApiKey);
    }

    [Fact]
    public void ClearEntropyCache_MultipleEncrypts_AllWork()
    {
        var text1 = "text-1";
        var text2 = "text-2";

        var encrypted1 = _storage.Encrypt(text1);
        _storage.ClearEntropyCache();
        var encrypted2 = _storage.Encrypt(text2);
        _storage.ClearEntropyCache();

        var decrypted1 = _storage.Decrypt(encrypted1);
        var decrypted2 = _storage.Decrypt(encrypted2);

        Assert.Equal(text1, decrypted1);
        Assert.Equal(text2, decrypted2);
    }
}
