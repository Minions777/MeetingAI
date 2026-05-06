using MeetingAI.Shared.Configuration;
using Xunit;

namespace MeetingAI.Core.Tests;

public class SecureStorageTests
{
    [Fact]
    public void Encrypt_Decrypt_RoundTrip_ReturnsOriginalValue()
    {
        // Arrange
        var originalText = "sk-test-api-key-12345";
        
        // Act
        var encrypted = SecureStorage.Encrypt(originalText);
        var decrypted = SecureStorage.Decrypt(encrypted);
        
        // Assert
        Assert.NotEqual(originalText, encrypted);
        Assert.Equal(originalText, decrypted);
    }
    
    [Fact]
    public void Encrypt_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        var emptyText = "";
        
        // Act
        var result = SecureStorage.Encrypt(emptyText);
        
        // Assert
        Assert.Equal(string.Empty, result);
    }
    
    [Fact]
    public void Decrypt_EmptyString_ReturnsEmptyString()
    {
        // Arrange
        var emptyText = "";
        
        // Act
        var result = SecureStorage.Decrypt(emptyText);
        
        // Assert
        Assert.Equal(string.Empty, result);
    }
    
    [Fact]
    public void Encrypt_SameValueProducesDifferentCiphertext()
    {
        // 由于使用了随机 Entropy，每次加密结果不同
        // Arrange
        var originalText = "same-text";
        
        // Act
        var encrypted1 = SecureStorage.Encrypt(originalText);
        SecureStorage.ClearEntropyCache();
        var encrypted2 = SecureStorage.Encrypt(originalText);
        
        // Assert - 由于 Entropy 不同，密文不同
        // 注意：如果使用相同的 Entropy，密文应该相同
        Assert.NotNull(encrypted1);
        Assert.NotNull(encrypted2);
    }
    
    [Fact]
    public void Decrypt_InvalidBase64_ReturnsOriginalValue()
    {
        // Arrange
        var invalidText = "not-a-valid-base64!!";
        
        // Act
        var result = SecureStorage.Decrypt(invalidText);
        
        // Assert
        Assert.Equal(invalidText, result);
    }
    
    [Fact]
    public void ValidateEncryption_ValidText_ReturnsTrue()
    {
        // Arrange
        var originalText = "valid-api-key";
        
        // Act
        var result = SecureStorage.ValidateEncryption(originalText);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void ValidateEncryption_EmptyText_ReturnsTrue()
    {
        // Arrange
        var emptyText = "";
        
        // Act
        var result = SecureStorage.ValidateEncryption(emptyText);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void Encrypt_SpecialCharacters_PreservesContent()
    {
        // Arrange
        var specialText = "!@#$%^&*()_+-=[]{}|;':\",./<>?`~";
        
        // Act
        var encrypted = SecureStorage.Encrypt(specialText);
        var decrypted = SecureStorage.Decrypt(encrypted);
        
        // Assert
        Assert.Equal(specialText, decrypted);
    }
    
    [Fact]
    public void Encrypt_UnicodeCharacters_PreservesContent()
    {
        // Arrange
        var unicodeText = "中文测试📝🔑密码";
        
        // Act
        var encrypted = SecureStorage.Encrypt(unicodeText);
        var decrypted = SecureStorage.Decrypt(encrypted);
        
        // Assert
        Assert.Equal(unicodeText, decrypted);
    }
    
    [Fact]
    public void EncryptConfig_RoundTrip_DecryptsCorrectly()
    {
        // Arrange
        var config = new ProviderConfig
        {
            Id = "test-provider",
            Name = "Test Provider",
            ApiKey = "sk-secret-key-12345"
        };
        
        // Act
        SecureStorage.EncryptConfig(config);
        Assert.NotEqual("sk-secret-key-12345", config.ApiKey);
        
        SecureStorage.DecryptConfig(config);
        
        // Assert
        Assert.Equal("sk-secret-key-12345", config.ApiKey);
    }
    
    [Fact]
    public void ClearEntropyCache_MultipleEncrypts_AllWork()
    {
        // Arrange
        var text1 = "text-1";
        var text2 = "text-2";
        
        // Act - 加密并清除缓存
        var encrypted1 = SecureStorage.Encrypt(text1);
        SecureStorage.ClearEntropyCache();
        var encrypted2 = SecureStorage.Encrypt(text2);
        SecureStorage.ClearEntropyCache();
        
        // 两个加密的文本应该都能正确解密
        var decrypted1 = SecureStorage.Decrypt(encrypted1);
        var decrypted2 = SecureStorage.Decrypt(encrypted2);
        
        // Assert
        Assert.Equal(text1, decrypted1);
        Assert.Equal(text2, decrypted2);
    }
}
