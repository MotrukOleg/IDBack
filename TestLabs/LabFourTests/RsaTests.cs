using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Dtos;
using WebApplication1.Services.RsaService;
using Xunit;

namespace TestLabs.LabFourTests;

public class RsaTests : IDisposable
{
    private readonly Mock<ILogger<RsaService>> _mockLogger;
    private readonly RsaService _rsaService;
    private readonly string _tempDirectory;

    public RsaTests()
    {
        _mockLogger = new Mock<ILogger<RsaService>>();
        _rsaService = new RsaService(_mockLogger.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"RsaServiceTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }
    

    [Theory]
    [InlineData(1024)]
    [InlineData(2048)]
    [InlineData(4096)]
    public async Task GenerateKeysAsync_ValidKeySize_ReturnsKeyPair(int keySize)
    {
        // Act
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync(keySize);

        // Assert
        Assert.NotNull(publicKey);
        Assert.NotNull(privateKey);
        Assert.NotNull(publicKey.PemKey);
        Assert.NotNull(privateKey.PemKey);
        Assert.Contains("-----BEGIN PUBLIC KEY-----", publicKey.PemKey);
        Assert.Contains("-----END PUBLIC KEY-----", publicKey.PemKey);
        Assert.Contains("-----BEGIN RSA PRIVATE KEY-----", privateKey.PemKey);
        Assert.Contains("-----END RSA PRIVATE KEY-----", privateKey.PemKey);
    }

    [Fact]
    public async Task GenerateKeysAsync_DefaultKeySize_Returns2048BitKeys()
    {
        // Act
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();

        // Assert
        Assert.NotNull(publicKey);
        Assert.NotNull(privateKey);
        Assert.True(publicKey.PemKey.Length > 300);
        Assert.True(privateKey.PemKey.Length > 1000);
    }

    [Theory]
    [InlineData(512)]
    [InlineData(768)]
    public async Task GenerateKeysAsync_SmallKeySize_GeneratesSuccessfully(int keySize)
    {
        // Act
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync(keySize);

        // Assert
        Assert.NotNull(publicKey);
        Assert.NotNull(privateKey);
    }
    
    [Fact]
    public async Task SavePublicKeyAsync_ValidKey_SavesSuccessfully()
    {
        // Arrange
        var (publicKey, _) = await _rsaService.GenerateKeysAsync();
        var filePath = Path.Combine(_tempDirectory, "test_public");

        // Act
        await _rsaService.SavePublicKeyAsync(publicKey, filePath);

        // Assert
        var expectedPath = Path.ChangeExtension(filePath, ".pem");
        Assert.True(File.Exists(expectedPath));
        var content = await File.ReadAllTextAsync(expectedPath);
        Assert.Equal(publicKey.PemKey, content);
    }

    [Fact]
    public async Task SavePrivateKeyAsync_ValidKey_SavesSuccessfully()
    {
        // Arrange
        var (_, privateKey) = await _rsaService.GenerateKeysAsync();
        var filePath = Path.Combine(_tempDirectory, "test_private");

        // Act
        await _rsaService.SavePrivateKeyAsync(privateKey, filePath);

        // Assert
        var expectedPath = Path.ChangeExtension(filePath, ".pem");
        Assert.True(File.Exists(expectedPath));
        var content = await File.ReadAllTextAsync(expectedPath);
        Assert.Equal(privateKey.PemKey, content);
    }

    [Fact]
    public async Task SavePublicKeyAsync_NullKey_HandlesAppropriately()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test_null");

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _rsaService.SavePublicKeyAsync(null!, filePath));
    }

    [Fact]
    public async Task LoadPublicKeyAsync_ExistingFile_LoadsSuccessfully()
    {
        // Arrange
        var (originalPublicKey, _) = await _rsaService.GenerateKeysAsync();
        var filePath = Path.Combine(_tempDirectory, "test_public.pem");
        await File.WriteAllTextAsync(filePath, originalPublicKey.PemKey);

        // Act
        var loadedKey = await _rsaService.LoadPublicKeyAsync(filePath);

        // Assert
        Assert.NotNull(loadedKey);
        Assert.Equal(originalPublicKey.PemKey, loadedKey.PemKey);
    }

    [Fact]
    public async Task LoadPublicKeyAsync_FileWithComments_LoadsCorrectly()
    {
        // Arrange
        var (originalPublicKey, _) = await _rsaService.GenerateKeysAsync();
        var filePath = Path.Combine(_tempDirectory, "test_public_with_comments.pem");
        var contentWithComments = $"# Created at: 2024-01-01 12:00:00 UTC\n{originalPublicKey.PemKey}";
        await File.WriteAllTextAsync(filePath, contentWithComments);

        // Act
        var loadedKey = await _rsaService.LoadPublicKeyAsync(filePath);

        // Assert
        Assert.NotNull(loadedKey);
        Assert.Equal(originalPublicKey.PemKey, loadedKey.PemKey);
    }

    [Fact]
    public async Task LoadPrivateKeyAsync_ExistingFile_LoadsSuccessfully()
    {
        // Arrange
        var (_, originalPrivateKey) = await _rsaService.GenerateKeysAsync();
        var filePath = Path.Combine(_tempDirectory, "test_private.pem");
        await File.WriteAllTextAsync(filePath, originalPrivateKey.PemKey);

        // Act
        var loadedKey = await _rsaService.LoadPrivateKeyAsync(filePath);

        // Assert
        Assert.NotNull(loadedKey);
        Assert.Equal(originalPrivateKey.PemKey, loadedKey.PemKey);
    }

    [Fact]
    public async Task LoadPublicKeyAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.pem");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _rsaService.LoadPublicKeyAsync(nonExistentPath));
    }

    [Fact]
    public async Task LoadPrivateKeyAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent_private.pem");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _rsaService.LoadPrivateKeyAsync(nonExistentPath));
    }

    [Fact]
    public async Task LoadPublicKeyAsync_EmptyFile_DoesNotThrow()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "empty.pem");
        await File.WriteAllTextAsync(filePath, string.Empty);

        // Act & Assert - No exception should be thrown
        var exception = await Record.ExceptionAsync(() =>
            _rsaService.LoadPublicKeyAsync(filePath));
    
        Assert.Null(exception);
    }
    

    [Fact]
    public async Task DeleteKeyAsync_ExistingFile_DeletesSuccessfully()
    {
        // Arrange
        var filePath = Path.Combine(_tempDirectory, "test_delete.pem");
        await File.WriteAllTextAsync(filePath, "test content");
        Assert.True(File.Exists(filePath));

        // Act
        await _rsaService.DeleteKeyAsync(filePath);

        // Assert
        Assert.False(File.Exists(filePath));
    }

    [Fact]
    public async Task DeleteKeyAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.pem");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _rsaService.DeleteKeyAsync(nonExistentPath));
    }
    [Fact]
    public async Task EncryptTextAsync_And_DecryptTextAsync_RoundTrip()
    {
        // Arrange
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
        var originalText = "Hello RSA encryption!";

        // Act
        var (encryptedText, encryptionTime) = await _rsaService.EncryptTextAsync(originalText, publicKey);
        var (decryptedText, decryptionTime) = await _rsaService.DecryptTextAsync(encryptedText, privateKey);

        // Assert
        Assert.NotNull(encryptedText);
        Assert.NotEqual(originalText, encryptedText);
        Assert.True(encryptionTime > 0);
        Assert.True(decryptionTime > 0);
        Assert.Equal(originalText, decryptedText);
    }

    [Fact]
    public async Task EncryptTextAsync_EmptyString_EncryptsSuccessfully()
    {
        // Arrange
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
        var originalText = "";

        // Act
        var (encryptedText, _) = await _rsaService.EncryptTextAsync(originalText, publicKey);
        var (decryptedText, _) = await _rsaService.DecryptTextAsync(encryptedText, privateKey);

        // Assert
        Assert.Equal(originalText, decryptedText);
    }

    [Theory]
    [InlineData("Hello World!")]
    [InlineData("Special chars: äöü ñ 中文 🌟")]
    [InlineData("Numbers: 12345")]
    [InlineData("Mixed: ABC123!@#")]
    public async Task EncryptDecryptText_VariousContent_WorksCorrectly(string originalText)
    {
        // Arrange
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();

        // Act
        var (encryptedText, _) = await _rsaService.EncryptTextAsync(originalText, publicKey);
        var (decryptedText, _) = await _rsaService.DecryptTextAsync(encryptedText, privateKey);

        // Assert
        Assert.Equal(originalText, decryptedText);
    }

    [Fact]
    public async Task EncryptTextAsync_InvalidPemKey_ThrowsException()
    {
        // Arrange
        var invalidKey = new RsaKeyDto { PemKey = "invalid pem content" };
        var text = "test";

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _rsaService.EncryptTextAsync(text, invalidKey));
    }

    [Fact]
    public async Task DecryptTextAsync_InvalidEncryptedText_ThrowsException()
    {
        // Arrange
        var (_, privateKey) = await _rsaService.GenerateKeysAsync();
        var invalidEncryptedText = "invalid base64";

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _rsaService.DecryptTextAsync(invalidEncryptedText, privateKey));
    }

    [Fact]
    public async Task DecryptTextAsync_WrongPrivateKey_ThrowsException()
    {
        // Arrange
        var (publicKey1, _) = await _rsaService.GenerateKeysAsync();
        var (_, privateKey2) = await _rsaService.GenerateKeysAsync();
        var text = "test message";

        var (encryptedText, _) = await _rsaService.EncryptTextAsync(text, publicKey1);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _rsaService.DecryptTextAsync(encryptedText, privateKey2));
    }

    [Fact]
    public async Task DecryptTextAsync_CorruptedData_ThrowsException()
    {
        // Arrange
        var (_, privateKey) = await _rsaService.GenerateKeysAsync();
        var text = "test";
        var (encryptedText, _) = await _rsaService.EncryptTextAsync(text, privateKey);

        // Corrupt the encrypted data
        var corruptedEncrypted = encryptedText.Substring(0, encryptedText.Length - 4) + "XXXX";

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _rsaService.DecryptTextAsync(corruptedEncrypted, privateKey));
    }
    

    [Fact]
    public async Task EncryptFileAsync_And_DecryptFileAsync_RoundTrip()
    {
        // Arrange
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
        var originalContent = "This is test file content for RSA encryption.";
        var originalBytes = Encoding.UTF8.GetBytes(originalContent);
        var fileName = "test.txt";
        var contentType = "text/plain";

        using var inputStream = new MemoryStream(originalBytes);

        // Act
        var (encryptedData, encryptionTime) = await _rsaService.EncryptFileAsync(
            inputStream, publicKey, fileName, contentType);

        using var encryptedStream = new MemoryStream(encryptedData);
        var (decryptedData, originalFileName, originalContentType, decryptionTime) =
            await _rsaService.DecryptFileAsync(encryptedStream, privateKey);

        // Assert
        Assert.NotNull(encryptedData);
        Assert.True(encryptedData.Length > originalBytes.Length);
        Assert.True(encryptionTime > 0);
        Assert.True(decryptionTime > 0);
        Assert.Equal(originalBytes, decryptedData);
        Assert.Equal(fileName, originalFileName);
        Assert.Equal(contentType, originalContentType);
    }

    [Fact]
    public async Task EncryptFileAsync_LargeFile_HandlesSuccessfully()
    {
        // Arrange
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
        var largeContent = new string('A', 10000);
        var originalBytes = Encoding.UTF8.GetBytes(largeContent);

        using var inputStream = new MemoryStream(originalBytes);

        // Act
        var (encryptedData, _) = await _rsaService.EncryptFileAsync(
            inputStream, publicKey, "large.txt", "text/plain");

        using var encryptedStream = new MemoryStream(encryptedData);
        var (decryptedData, _, _, _) = await _rsaService.DecryptFileAsync(encryptedStream, privateKey);

        // Assert
        Assert.Equal(originalBytes, decryptedData);
    }

    [Fact]
    public async Task EncryptFileAsync_NullContentType_UsesDefault()
    {
        // Arrange
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
        var content = "test content";
        var bytes = Encoding.UTF8.GetBytes(content);

        using var inputStream = new MemoryStream(bytes);

        // Act
        var (encryptedData, _) = await _rsaService.EncryptFileAsync(
            inputStream, publicKey, "test.txt", null);

        using var encryptedStream = new MemoryStream(encryptedData);
        var (_, _, contentType, _) = await _rsaService.DecryptFileAsync(encryptedStream, privateKey);

        // Assert
        Assert.Equal("application/octet-stream", contentType);
    }

    [Fact]
    public async Task EncryptFileAsync_EmptyFile_HandlesSuccessfully()
    {
        // Arrange
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
        var originalBytes = Array.Empty<byte>();

        using var inputStream = new MemoryStream(originalBytes);

        // Act
        var (encryptedData, _) = await _rsaService.EncryptFileAsync(
            inputStream, publicKey, "empty.txt", "text/plain");

        using var encryptedStream = new MemoryStream(encryptedData);
        var (decryptedData, _, _, _) = await _rsaService.DecryptFileAsync(encryptedStream, privateKey);

        // Assert
        Assert.Equal(originalBytes, decryptedData);
    }

    [Fact]
    public async Task EncryptFileAsync_BinaryFile_HandlesSuccessfully()
    {
        // Arrange
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
        var originalBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }; // JPEG header

        using var inputStream = new MemoryStream(originalBytes);

        // Act
        var (encryptedData, _) = await _rsaService.EncryptFileAsync(
            inputStream, publicKey, "image.jpg", "image/jpeg");

        using var encryptedStream = new MemoryStream(encryptedData);
        var (decryptedData, _, contentType, _) = await _rsaService.DecryptFileAsync(encryptedStream, privateKey);

        // Assert
        Assert.Equal(originalBytes, decryptedData);
        Assert.Equal("image/jpeg", contentType);
    }

    [Fact]
    public async Task EncryptFileAsync_InvalidPublicKey_ThrowsException()
    {
        // Arrange
        var invalidKey = new RsaKeyDto { PemKey = "invalid pem" };
        var bytes = Encoding.UTF8.GetBytes("test");

        using var inputStream = new MemoryStream(bytes);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _rsaService.EncryptFileAsync(inputStream, invalidKey, "test.txt", "text/plain"));
    }

    [Fact]
    public async Task DecryptFileAsync_InvalidPrivateKey_ThrowsException()
    {
        // Arrange
        var (publicKey, _) = await _rsaService.GenerateKeysAsync();
        var invalidPrivateKey = new RsaKeyDto { PemKey = "invalid pem" };
        var bytes = Encoding.UTF8.GetBytes("test");

        using var inputStream = new MemoryStream(bytes);
        var (encryptedData, _) = await _rsaService.EncryptFileAsync(
            inputStream, publicKey, "test.txt", "text/plain");

        using var encryptedStream = new MemoryStream(encryptedData);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _rsaService.DecryptFileAsync(encryptedStream, invalidPrivateKey));
    }

    [Fact]
    public async Task DecryptFileAsync_CorruptedData_ThrowsException()
    {
        // Arrange
        var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
        var bytes = Encoding.UTF8.GetBytes("test");

        using var inputStream = new MemoryStream(bytes);
        var (encryptedData, _) = await _rsaService.EncryptFileAsync(
            inputStream, publicKey, "test.txt", "text/plain");

        // Corrupt the data
        encryptedData[encryptedData.Length / 2] ^= 0xFF;

        using var encryptedStream = new MemoryStream(encryptedData);

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _rsaService.DecryptFileAsync(encryptedStream, privateKey));
    }
}