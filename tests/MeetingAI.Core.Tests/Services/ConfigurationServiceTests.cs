using FluentAssertions;
using MeetingAI.Core.Tests.Helpers;
using MeetingAI.Shared.Configuration;
using Moq;
using Xunit;

namespace MeetingAI.Core.Tests.Services;

public class ConfigurationServiceTests
{
    private readonly Mock<ISecureStorage> _secureStorageMock;
    private readonly string _tempConfigPath;

    public ConfigurationServiceTests()
    {
        _secureStorageMock = new Mock<ISecureStorage>();
        _secureStorageMock.Setup(x => x.Encrypt(It.IsAny<string>())).Returns<string>(s => $"enc:{s}");
        _secureStorageMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns<string>(s =>
            s.StartsWith("enc:") ? s[4..] : s);
        _tempConfigPath = Path.Combine(Path.GetTempPath(), $"MeetingAI_test_{Guid.NewGuid():N}");
    }

    [Fact]
    public void Load_WhenFileNotExist_CreatesDefault()
    {
        var sut = new ConfigurationService(_secureStorageMock.Object);
        var result = sut.Load();
        result.Should().NotBeNull();
        result.Providers.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoadAsync_WhenFileNotExist_CreatesDefault()
    {
        var sut = new ConfigurationService(_secureStorageMock.Object);
        var result = await sut.LoadAsync();
        result.Should().NotBeNull();
    }

    [Fact]
    public void ClearCache_ForcesReload()
    {
        var sut = new ConfigurationService(_secureStorageMock.Object);
        var first = sut.Load();
        sut.ClearCache();
        var second = sut.Load();
        second.Should().NotBeNull();
    }

    [Fact]
    public void ExportSafe_RedactsSensitiveFields()
    {
        var sut = new ConfigurationService(_secureStorageMock.Object);
        var result = sut.ExportSafe();
        result.Should().Contain("***REDACTED***");
        result.Should().NotContain("test-key");
    }

    [Fact]
    public void ValidateConfiguration_WithDefaultProviders_Succeeds()
    {
        var sut = new ConfigurationService(_secureStorageMock.Object);
        var (isValid, errors) = sut.ValidateConfiguration();
        isValid.Should().BeTrue();
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Backup_CreatesBackupFile()
    {
        var sut = new ConfigurationService(_secureStorageMock.Object);
        sut.Load();
        sut.Backup();
        var backups = sut.GetBackupFiles();
        backups.Should().NotBeEmpty();
    }

    [Fact]
    public void GetBackupPath_ReturnsValidPath()
    {
        var sut = new ConfigurationService(_secureStorageMock.Object);
        var path = sut.GetBackupPath();
        path.Should().NotBeNullOrEmpty();
        path.Should().EndWith(".json");
    }

    [Fact]
    public void Reload_ReturnsSettings()
    {
        var sut = new ConfigurationService(_secureStorageMock.Object);
        var result = sut.Reload();
        result.Should().NotBeNull();
    }
}
