namespace MeetingAI.Shared.Configuration;

public interface ISecureStorage
{
    string Encrypt(string plainText);
    string Decrypt(string encryptedText);
    void EncryptConfig(ProviderConfig config);
    void DecryptConfig(ProviderConfig config);
    bool ValidateEncryption(string originalText);
    void ClearEntropyCache();
}
