using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using WebApplication1.Dtos;
using WebApplication1.Services.RsaService;
using Xunit;

namespace TestLabs.LabFourTests
{
    public class RsaTests : IDisposable
    {
        private readonly Mock<ILogger<RsaService>> _mockLogger;
        private readonly RsaService _rsaService;
        private readonly string _tempDirectory;

        public RsaTests()
        {
            _mockLogger = new Mock<ILogger<RsaService>>();
            _rsaService = new RsaService(_mockLogger.Object);
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
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
            var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync(keySize);

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
            var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();

            Assert.NotNull(publicKey);
            Assert.NotNull(privateKey);
            Assert.True(publicKey.PemKey.Length > 300);
            Assert.True(privateKey.PemKey.Length > 1000);
        }

        [Fact]
        public async Task SavePublicKeyAsync_ValidKey_SavesSuccessfully()
        {
            var (publicKey, _) = await _rsaService.GenerateKeysAsync();
            var filePath = Path.Combine(_tempDirectory, "test_public");

            await _rsaService.SavePublicKeyAsync(publicKey, filePath);

            var expectedPath = Path.ChangeExtension(filePath, ".pem");
            Assert.True(File.Exists(expectedPath));
            var content = await File.ReadAllTextAsync(expectedPath);
            Assert.Equal(publicKey.PemKey, content);
        }

        [Fact]
        public async Task SavePrivateKeyAsync_ValidKey_SavesSuccessfully()
        {
            var (_, privateKey) = await _rsaService.GenerateKeysAsync();
            var filePath = Path.Combine(_tempDirectory, "test_private");

            await _rsaService.SavePrivateKeyAsync(privateKey, filePath);

            var expectedPath = Path.ChangeExtension(filePath, ".pem");
            Assert.True(File.Exists(expectedPath));
            var content = await File.ReadAllTextAsync(expectedPath);
            Assert.Equal(privateKey.PemKey, content);
        }

        [Fact]
        public async Task LoadPublicKeyAsync_ExistingFile_LoadsSuccessfully()
        {
            var (originalPublicKey, _) = await _rsaService.GenerateKeysAsync();
            var filePath = Path.Combine(_tempDirectory, "test_public.pem");
            await File.WriteAllTextAsync(filePath, originalPublicKey.PemKey);

            var loadedKey = await _rsaService.LoadPublicKeyAsync(filePath);

            Assert.NotNull(loadedKey);
            Assert.Equal(originalPublicKey.PemKey, loadedKey.PemKey);
        }

        [Fact]
        public async Task LoadPublicKeyAsync_FileWithComments_LoadsCorrectly()
        {
            var (originalPublicKey, _) = await _rsaService.GenerateKeysAsync();
            var filePath = Path.Combine(_tempDirectory, "test_public_with_comments.pem");
            var contentWithComments = $"# Created at: 2024-01-01 12:00:00 UTC\n{originalPublicKey.PemKey}";
            await File.WriteAllTextAsync(filePath, contentWithComments);

            var loadedKey = await _rsaService.LoadPublicKeyAsync(filePath);

            Assert.NotNull(loadedKey);
            Assert.Equal(originalPublicKey.PemKey, loadedKey.PemKey);
        }

        [Fact]
        public async Task LoadPrivateKeyAsync_ExistingFile_LoadsSuccessfully()
        {
            var (_, originalPrivateKey) = await _rsaService.GenerateKeysAsync();
            var filePath = Path.Combine(_tempDirectory, "test_private.pem");
            await File.WriteAllTextAsync(filePath, originalPrivateKey.PemKey);

            var loadedKey = await _rsaService.LoadPrivateKeyAsync(filePath);

            Assert.NotNull(loadedKey);
            Assert.Equal(originalPrivateKey.PemKey, loadedKey.PemKey);
        }

        [Fact]
        public async Task LoadPublicKeyAsync_NonExistentFile_ThrowsFileNotFoundException()
        {
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.pem");

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                _rsaService.LoadPublicKeyAsync(nonExistentPath));
        }

        [Fact]
        public async Task DeleteKeyAsync_ExistingFile_DeletesSuccessfully()
        {
            var filePath = Path.Combine(_tempDirectory, "test_delete.pem");
            await File.WriteAllTextAsync(filePath, "test content");
            Assert.True(File.Exists(filePath));

            await _rsaService.DeleteKeyAsync(filePath);

            Assert.False(File.Exists(filePath));
        }

        [Fact]
        public async Task DeleteKeyAsync_NonExistentFile_ThrowsFileNotFoundException()
        {
            var nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.pem");

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                _rsaService.DeleteKeyAsync(nonExistentPath));
        }

        [Fact]
        public async Task EncryptTextAsync_And_DecryptTextAsync_RoundTrip()
        {
            var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
            var originalText = "Hello RSA encryption!";

            var (encryptedText, encryptionTime) = await _rsaService.EncryptTextAsync(originalText, publicKey);
            var (decryptedText, decryptionTime) = await _rsaService.DecryptTextAsync(encryptedText, privateKey);

            Assert.NotNull(encryptedText);
            Assert.NotEqual(originalText, encryptedText);
            Assert.True(encryptionTime > 0);
            Assert.True(decryptionTime > 0);
            Assert.Equal(originalText, decryptedText);
        }

        [Fact]
        public async Task EncryptTextAsync_EmptyString_EncryptsSuccessfully()
        {
            var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
            var originalText = "";

            var (encryptedText, _) = await _rsaService.EncryptTextAsync(originalText, publicKey);
            var (decryptedText, _) = await _rsaService.DecryptTextAsync(encryptedText, privateKey);

            Assert.Equal(originalText, decryptedText);
        }

        [Fact]
        public async Task EncryptFileAsync_And_DecryptFileAsync_RoundTrip()
        {
            var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
            var originalContent = "This is test file content for RSA encryption.";
            var originalBytes = Encoding.UTF8.GetBytes(originalContent);
            var fileName = "test.txt";
            var contentType = "text/plain";

            using var inputStream = new MemoryStream(originalBytes);

            var (encryptedData, encryptionTime) = await _rsaService.EncryptFileAsync(
                inputStream, publicKey, fileName, contentType);

            using var encryptedStream = new MemoryStream(encryptedData);
            var (decryptedData, originalFileName, originalContentType, decryptionTime) =
                await _rsaService.DecryptFileAsync(encryptedStream, privateKey);

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
            var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
            var largeContent = new string('A', 10000);
            var originalBytes = Encoding.UTF8.GetBytes(largeContent);

            using var inputStream = new MemoryStream(originalBytes);

            var (encryptedData, _) = await _rsaService.EncryptFileAsync(
                inputStream, publicKey, "large.txt", "text/plain");

            using var encryptedStream = new MemoryStream(encryptedData);
            var (decryptedData, _, _, _) = await _rsaService.DecryptFileAsync(encryptedStream, privateKey);

            Assert.Equal(originalBytes, decryptedData);
        }

        [Fact]
        public async Task EncryptTextAsync_InvalidPemKey_ThrowsException()
        {
            var invalidKey = new RsaKeyDto { PemKey = "invalid pem content" };
            var text = "test";


            await Assert.ThrowsAnyAsync<Exception>(() =>
                _rsaService.EncryptTextAsync(text, invalidKey));
        }

        [Fact]
        public async Task DecryptTextAsync_InvalidEncryptedText_ThrowsException()
        {
            var (_, privateKey) = await _rsaService.GenerateKeysAsync();
            var invalidEncryptedText = "invalid base64";

            await Assert.ThrowsAnyAsync<Exception>(() =>
                _rsaService.DecryptTextAsync(invalidEncryptedText, privateKey));
        }

        [Fact]
        public async Task DecryptTextAsync_WrongPrivateKey_ThrowsException()
        {
            var (publicKey1, _) = await _rsaService.GenerateKeysAsync();
            var (_, privateKey2) = await _rsaService.GenerateKeysAsync();
            var text = "test message";

            var (encryptedText, _) = await _rsaService.EncryptTextAsync(text, publicKey1);

            await Assert.ThrowsAnyAsync<Exception>(() =>
                _rsaService.DecryptTextAsync(encryptedText, privateKey2));
        }

        [Fact]
        public async Task EncryptFileAsync_NullContentType_UsesDefault()
        {
            var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();
            var content = "test content";
            var bytes = Encoding.UTF8.GetBytes(content);

            using var inputStream = new MemoryStream(bytes);

            var (encryptedData, _) = await _rsaService.EncryptFileAsync(
                inputStream, publicKey, "test.txt", null);

            using var encryptedStream = new MemoryStream(encryptedData);
            var (_, _, contentType, _) = await _rsaService.DecryptFileAsync(encryptedStream, privateKey);

            Assert.Equal("application/octet-stream", contentType);
        }

        [Theory]
        [InlineData("Hello World!")]
        [InlineData("Special chars: äöü ñ 中文 🌟")]
        [InlineData("Numbers: 12345")]
        [InlineData("Mixed: ABC123!@#")]
        public async Task EncryptDecryptText_VariousContent_WorksCorrectly(string originalText)
        {
            var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync();

            var (encryptedText, _) = await _rsaService.EncryptTextAsync(originalText, publicKey);
            var (decryptedText, _) = await _rsaService.DecryptTextAsync(encryptedText, privateKey);

            Assert.Equal(originalText, decryptedText);
        }
    }
}

