using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Dtos;
using WebApplication1.Services.DssService;

namespace TestLabs.LabFileTests;

public class DssTests
{
    private readonly Mock<ILogger<DssService>> _mockLogger;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly DssService _dssService;
    private readonly string _tempDirectory;

    public DssTests()
    {
        _mockLogger = new Mock<ILogger<DssService>>();
        _mockConfiguration = new Mock<IConfiguration>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"DssServiceTests_{Guid.NewGuid()}");
        
        _mockConfiguration
            .Setup(c => c["KeysDirectory"])
            .Returns(_tempDirectory);

        _dssService = new DssService(_mockLogger.Object, _mockConfiguration.Object);
    }

    [Fact]
    public void Constructor_CreatesKeysDirectory_IfNotExists()
    {
        // Assert
        Assert.True(Directory.Exists(_tempDirectory));
    }

    [Fact]
    public void GetPublicKey_ReturnsNull_WhenNotGenerated()
    {
        // Act
        var result = _dssService.GetPublicKey();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetPrivateKey_ReturnsNull_WhenNotGenerated()
    {
        // Act
        var result = _dssService.GetPrivateKey();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void HasPrivateKey_ReturnsFalse_WhenNotGenerated()
    {
        // Act
        var result = _dssService.HasPrivateKey();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasPublicKey_ReturnsFalse_WhenNotGenerated()
    {
        // Act
        var result = _dssService.HasPublicKey();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GenerateKeys_SuccessfullyGeneratesAndSavesKeys()
    {
        // Act
        var result = await _dssService.GenerateKeys();

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Keys);
        Assert.NotEmpty(result.Keys.PublicKey);
        Assert.NotEmpty(result.Keys.PrivateKey);
        Assert.True(_dssService.HasPublicKey());
        Assert.True(_dssService.HasPrivateKey());
    }

    [Fact]
    public async Task GenerateKeys_SavesKeysToFiles()
    {
        // Act
        var result = await _dssService.GenerateKeys();

        // Assert
        Assert.True(result.Success);
        var publicKeyPath = Path.Combine(_tempDirectory, result.PublicKeyFileName!);
        var privateKeyPath = Path.Combine(_tempDirectory, result.PrivateKeyFileName!);
        
        Assert.True(File.Exists(publicKeyPath));
        Assert.True(File.Exists(privateKeyPath));
    }

    [Fact]
    public async Task GenerateKeys_WithCustomFileNames_SavesWithCorrectNames()
    {
        // Arrange
        const string publicKeyFileName = "custom_public.pem";
        const string privateKeyFileName = "custom_private.pem";

        // Act
        var result = await _dssService.GenerateKeys(publicKeyFileName, privateKeyFileName);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(publicKeyFileName, result.PublicKeyFileName);
        Assert.Equal(privateKeyFileName, result.PrivateKeyFileName);
    }

    [Fact]
    public void ImportKey_FailsWithEmptyKeyPem()
    {
        // Arrange
        var request = new KeyImportRequestDto { KeyPem = string.Empty, IsPrivateKey = false };

        // Act
        var result = _dssService.ImportKey(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("порожнім", result.Message);
    }

    [Fact]
    public async Task ImportKey_SuccessfullyImportsPrivateKey()
    {
        // Arrange
        var generatedKeys = await _dssService.GenerateKeys();
        var dssService2 = new DssService(_mockLogger.Object, _mockConfiguration.Object);

        // Act
        var result = dssService2.ImportKey(new KeyImportRequestDto
        {
            KeyPem = generatedKeys.Keys!.PrivateKey,
            IsPrivateKey = true
        });

        // Assert
        Assert.True(result.Success);
        Assert.True(dssService2.HasPrivateKey());
        Assert.True(dssService2.HasPublicKey());
    }

    [Fact]
    public async Task ImportKey_SuccessfullyImportsPublicKey()
    {
        // Arrange
        var generatedKeys = await _dssService.GenerateKeys();
        var dssService2 = new DssService(_mockLogger.Object, _mockConfiguration.Object);

        // Act
        var result = dssService2.ImportKey(new KeyImportRequestDto
        {
            KeyPem = generatedKeys.Keys!.PublicKey,
            IsPrivateKey = false
        });

        // Assert
        Assert.True(result.Success);
        Assert.True(dssService2.HasPublicKey());
    }

    [Fact]
    public async Task LoadKeyFromServer_LoadsExistingKeyFile()
    {
        // Arrange
        var generatedKeys = await _dssService.GenerateKeys("pub_key.pem", "priv_key.pem");

        // Act
        var result = await _dssService.LoadKeyFromServer("pub_key.pem", false);

        // Assert
        Assert.True(result.Success);
    }

    [Fact]
    public async Task LoadKeyFromServer_FailsWithNonexistentFile()
    {
        // Act
        var result = await _dssService.LoadKeyFromServer("nonexistent.pem", false);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task DownloadKeyFromServer_ReturnsFileContent()
    {
        // Arrange
        var generatedKeys = await _dssService.GenerateKeys("download_key.pem", "download_priv.pem");

        // Act
        var result = await _dssService.DownloadKeyFromServer("download_key.pem");

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.FileContent);
        Assert.NotEmpty(result.FileContent);
        Assert.Equal("download_key.pem", result.FileName);
    }

    [Fact]
    public async Task DownloadKeyFromServer_FailsWithNonexistentFile()
    {
        // Act
        var result = await _dssService.DownloadKeyFromServer("nonexistent.pem");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task GetAvailableKeyFiles_ReturnsListOfPemFiles()
    {
        // Arrange
        await _dssService.GenerateKeys("key1.pem", "key1_priv.pem");
        await _dssService.GenerateKeys("key2.pem", "key2_priv.pem");

        // Act
        var result = _dssService.GetAvailableKeyFiles();

        // Assert
        Assert.NotEmpty(result);
        Assert.True(result.Count >= 2);
        Assert.All(result, file => Assert.EndsWith(".pem", file));
    }

    [Fact]
    public async Task DeleteKeyFile_RemovesFileSuccessfully()
    {
        // Arrange
        await _dssService.GenerateKeys("delete_key.pem", "delete_priv.pem");
        var filePath = Path.Combine(_tempDirectory, "delete_key.pem");
        Assert.True(File.Exists(filePath));

        // Act
        var result = await _dssService.DeleteKeyFile("delete_key.pem");

        // Assert
        Assert.True(result.Success);
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteKeyFile_FailsWithNonexistentFile()
    {
        // Act
        var result = await _dssService.DeleteKeyFile("nonexistent.pem");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message);
    }

    [Fact]
    public async Task SignText_FailsWithoutPrivateKey()
    {
        // Arrange
        var request = new SignatureRequestDto { Text = "Test text" };

        // Act
        var result = _dssService.SignText(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Приватний ключ", result.Message);
    }

    [Fact]
    public async Task SignText_SuccessfullySignsText()
    {
        // Arrange
        await _dssService.GenerateKeys();
        var request = new SignatureRequestDto { Text = "Test text" };

        // Act
        var result = _dssService.SignText(request);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.SignatureHex);
        Assert.True(result.SignatureLength > 0);
        Assert.Equal("DSA", result.Algorithm);
    }

    [Fact]
    public async Task SignText_FailsWithEmptyText()
    {
        // Arrange
        await _dssService.GenerateKeys();
        var request = new SignatureRequestDto { Text = string.Empty };

        // Act
        var result = _dssService.SignText(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("порожнім", result.Message);
    }

    [Fact]
    public async Task VerifyText_FailsWithoutPublicKey()
    {
        // Arrange
        var request = new VerifyRequestDto { Text = "Test", SignatureHex = "ABC123" };

        // Act
        var result = _dssService.VerifyText(request);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.IsValid);
        Assert.Contains("Публічний ключ", result.Message);
    }

    [Fact]
    public async Task VerifyText_SuccessfullyVerifiesValidSignature()
    {
        // Arrange
        await _dssService.GenerateKeys();
        var text = "Test text";
        var signRequest = new SignatureRequestDto { Text = text };
        var signResult = _dssService.SignText(signRequest);

        // Act
        var verifyRequest = new VerifyRequestDto { Text = text, SignatureHex = signResult.SignatureHex };
        var verifyResult = _dssService.VerifyText(verifyRequest);

        // Assert
        Assert.True(verifyResult.Success);
        Assert.True(verifyResult.IsValid);
        Assert.Contains("дійсний", verifyResult.Message.ToLower());
    }

    [Fact]
    public async Task VerifyText_FailsWithModifiedText()
    {
        // Arrange
        await _dssService.GenerateKeys();
        var text = "Test text";
        var signRequest = new SignatureRequestDto { Text = text };
        var signResult = _dssService.SignText(signRequest);

        // Act
        var verifyRequest = new VerifyRequestDto { Text = "Modified text", SignatureHex = signResult.SignatureHex };
        var verifyResult = _dssService.VerifyText(verifyRequest);

        // Assert
        Assert.True(verifyResult.Success);
        Assert.False(verifyResult.IsValid);
        Assert.Contains("недійсний", verifyResult.Message.ToLower());
    }

    [Fact]
    public async Task SignFile_SuccessfullySignsFile()
    {
        // Arrange
        await _dssService.GenerateKeys();
        var fileContent = new byte[] { 1, 2, 3, 4, 5 };
        var formFile = CreateMockFormFile(fileContent, "test.txt");

        // Act
        var result = await _dssService.SignFile(formFile);

        // Assert
        Assert.True(result.Success);
        Assert.NotEmpty(result.SignatureHex);
        Assert.True(result.SignatureLength > 0);
    }

    [Fact]
    public async Task SignFile_FailsWithEmptyFile()
    {
        // Arrange
        await _dssService.GenerateKeys();
        var formFile = CreateMockFormFile(Array.Empty<byte>(), "empty.txt");

        // Act
        var result = await _dssService.SignFile(formFile);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task VerifyFile_SuccessfullyVerifiesValidFileSignature()
    {
        // Arrange
        await _dssService.GenerateKeys();
        var fileContent = new byte[] { 1, 2, 3, 4, 5 };
        var formFile = CreateMockFormFile(fileContent, "test.txt");
        var signResult = await _dssService.SignFile(formFile);

        var verifyFile = CreateMockFormFile(fileContent, "test.txt");

        // Act
        var verifyResult = await _dssService.VerifyFile(verifyFile, signResult.SignatureHex);

        // Assert
        Assert.True(verifyResult.Success);
        Assert.True(verifyResult.IsValid);
    }

    [Fact]
    public async Task VerifyFile_FailsWithModifiedFile()
    {
        // Arrange
        await _dssService.GenerateKeys();
        var fileContent = new byte[] { 1, 2, 3, 4, 5 };
        var formFile = CreateMockFormFile(fileContent, "test.txt");
        var signResult = await _dssService.SignFile(formFile);

        var modifiedFileContent = new byte[] { 1, 2, 3, 4, 6 };
        var modifiedFile = CreateMockFormFile(modifiedFileContent, "test.txt");

        // Act
        var verifyResult = await _dssService.VerifyFile(modifiedFile, signResult.SignatureHex);

        // Assert
        Assert.True(verifyResult.Success);
        Assert.False(verifyResult.IsValid);
    }

    [Theory]
    [InlineData("ABCDEF123456")]
    [InlineData("abcdef123456")]
    [InlineData("AB-CD-EF-12-34-56")]
    public void HexToBytes_SuccessfullyConvertsValidHex(string hex)
    {
        // Act & Assert
        Assert.NotNull(_dssService.GetType()
            .GetMethod("HexToBytes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.Invoke(_dssService, new object[] { hex }));
    }

    [Theory]
    [InlineData("ABCDEFG")]
    [InlineData("ABC")]
    public void HexToBytes_ThrowsWithInvalidHex(string hex)
    {
        // Act & Assert
        var exception = Assert.Throws<TargetInvocationException>(() =>
            _dssService.GetType()
                .GetMethod("HexToBytes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.Invoke(_dssService, new object[] { hex })
        );
    
        Assert.NotNull(exception.InnerException);
        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    private static IFormFile CreateMockFormFile(byte[] content, string fileName)
    {
        var stream = new MemoryStream(content);
        var fileMock = new Mock<IFormFile>();

        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(content.Length);
        fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
        fileMock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns((Stream s, CancellationToken ct) =>
            {
                stream.Position = 0;
                return stream.CopyToAsync(s, 81920, ct);
            });

        return fileMock.Object;
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
}