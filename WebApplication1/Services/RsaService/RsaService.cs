using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using WebApplication1.Dtos;
using WebApplication1.Interfaces.IRsaService;

namespace WebApplication1.Services.RsaService
{
    public class RsaService : IRsaService
    {
        private readonly ILogger<RsaService> _logger;
        private const int HeaderSize = 12;

        public RsaService(ILogger<RsaService> logger)
        {
            _logger = logger;
        }

        public async Task<(RsaKeyDto publicKey, RsaKeyDto privateKey)> GenerateKeysAsync(int keySize = 2048)
        {
            return await Task.Run(() =>
            {
                using var rsa = RSA.Create(keySize);

                string publicKeyPem = rsa.ExportSubjectPublicKeyInfoPem();
                string privateKeyPem = rsa.ExportRSAPrivateKeyPem();

                var publicKey = new RsaKeyDto
                {
                    PemKey = publicKeyPem,
                };

                var privateKey = new RsaKeyDto
                {
                    PemKey = privateKeyPem,
                };

                return (publicKey, privateKey);
            });
        }

        public async Task SavePublicKeyAsync(RsaKeyDto key, string filePath)
        {
            string pemFilePath = Path.ChangeExtension(filePath, ".pem");
            await File.WriteAllTextAsync(pemFilePath, key.PemKey);
        }

        public async Task SavePrivateKeyAsync(RsaKeyDto privateKey, string filePath)
        {
            string pemFilePath = Path.ChangeExtension(filePath, ".pem");
            await File.WriteAllTextAsync(pemFilePath, privateKey.PemKey);
        }

        public async Task<RsaKeyDto> LoadPublicKeyAsync(string filePath)
        {
            string content = await File.ReadAllTextAsync(filePath);
            content = RemoveMetadataComment(content);

            return new RsaKeyDto
            {
                PemKey = content.Trim()
            };
        }

        public async Task<RsaKeyDto> LoadPrivateKeyAsync(string filePath)
        {
            string content = await File.ReadAllTextAsync(filePath);
            content = RemoveMetadataComment(content);

            return new RsaKeyDto
            {
                PemKey = content.Trim()
            };
        }

        private string RemoveMetadataComment(string content)
        {
            var lines = content.Split('\n');
            if (lines.Length > 0 && lines[0].StartsWith("# Created at:"))
            {
                content = string.Join('\n', lines.Skip(1));
            }
            return content;
        }

        public async Task DeleteKeyAsync(string filePath)
        {
            await Task.Run(() =>
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("Key file not found");
                }

                try
                {
                    File.Delete(filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting key file");
                    throw;
                }
            });
        }

        public async Task<(byte[] encryptedData, double encryptionTime)> EncryptFileAsync(
            Stream inputStream,
            RsaKeyDto publicKey,
            string originalFileName,
            string contentType)
        {
            var stopwatch = Stopwatch.StartNew();

            return await Task.Run(() =>
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKey.PemKey);

                using var aes = Aes.Create();
                aes.KeySize = 256;
                aes.GenerateKey();
                aes.GenerateIV();

                byte[] encryptedAesKey = rsa.Encrypt(aes.Key, RSAEncryptionPadding.OaepSHA256);
                byte[] encryptedAesIV = rsa.Encrypt(aes.IV, RSAEncryptionPadding.OaepSHA256);

                var metadata = new FileMetadata
                {
                    OriginalFileName = originalFileName,
                    ContentType = contentType ?? "application/octet-stream",
                    EncryptedAt = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                };

                string metadataJson = System.Text.Json.JsonSerializer.Serialize(metadata);
                byte[] metadataBytes = Encoding.UTF8.GetBytes(metadataJson);

                using var outputStream = new MemoryStream();

                byte[] keyLengthBytes = BitConverter.GetBytes(encryptedAesKey.Length);
                byte[] ivLengthBytes = BitConverter.GetBytes(encryptedAesIV.Length);
                byte[] metadataLengthBytes = BitConverter.GetBytes(metadataBytes.Length);

                outputStream.Write(keyLengthBytes, 0, 4);
                outputStream.Write(ivLengthBytes, 0, 4);
                outputStream.Write(metadataLengthBytes, 0, 4);

                outputStream.Write(encryptedAesKey, 0, encryptedAesKey.Length);
                outputStream.Write(encryptedAesIV, 0, encryptedAesIV.Length);

                outputStream.Write(metadataBytes, 0, metadataBytes.Length);

                using (var cs = new CryptoStream(outputStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    inputStream.CopyTo(cs);
                }

                stopwatch.Stop();

                return (outputStream.ToArray(), stopwatch.Elapsed.TotalMilliseconds);
            });
        }

        public async Task<(byte[] decryptedData, string originalFileName, string contentType, double decryptionTime)> DecryptFileAsync(
            Stream inputStream,
            RsaKeyDto privateKey)
        {
            var stopwatch = Stopwatch.StartNew();

            return await Task.Run(() =>
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(privateKey.PemKey);

                byte[] keyLengthBytes = new byte[4];
                byte[] ivLengthBytes = new byte[4];
                byte[] metadataLengthBytes = new byte[4];

                if (!ReadExactly(inputStream, keyLengthBytes, 4))
                {
                    throw new InvalidOperationException("Failed to read key length from encrypted file");
                }

                if (!ReadExactly(inputStream, ivLengthBytes, 4))
                {
                    throw new InvalidOperationException("Failed to read IV length from encrypted file");
                }

                if (!ReadExactly(inputStream, metadataLengthBytes, 4))
                {
                    throw new InvalidOperationException("Failed to read metadata length from encrypted file");
                }

                int keyLength = BitConverter.ToInt32(keyLengthBytes, 0);
                int ivLength = BitConverter.ToInt32(ivLengthBytes, 0);
                int metadataLength = BitConverter.ToInt32(metadataLengthBytes, 0);

                if (keyLength <= 0 || keyLength > 4096)
                {
                    throw new InvalidOperationException($"Invalid key length: {keyLength}");
                }

                if (ivLength <= 0 || ivLength > 256)
                {
                    throw new InvalidOperationException($"Invalid IV length: {ivLength}");
                }

                if (metadataLength <= 0 || metadataLength > 10485760)
                {
                    throw new InvalidOperationException($"Invalid metadata length: {metadataLength}");
                }

                byte[] encryptedAesKey = new byte[keyLength];
                byte[] encryptedAesIV = new byte[ivLength];
                byte[] metadataBytes = new byte[metadataLength];

                if (!ReadExactly(inputStream, encryptedAesKey, keyLength))
                {
                    throw new InvalidOperationException("Failed to read encrypted AES key from file");
                }

                if (!ReadExactly(inputStream, encryptedAesIV, ivLength))
                {
                    throw new InvalidOperationException("Failed to read encrypted AES IV from file");
                }

                if (!ReadExactly(inputStream, metadataBytes, metadataLength))
                {
                    throw new InvalidOperationException("Failed to read metadata from file");
                }

                byte[] aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);
                byte[] aesIV = rsa.Decrypt(encryptedAesIV, RSAEncryptionPadding.OaepSHA256);

                string metadataJson = Encoding.UTF8.GetString(metadataBytes);
                var metadata = System.Text.Json.JsonSerializer.Deserialize<FileMetadata>(metadataJson);

                if (metadata == null)
                {
                    throw new InvalidOperationException("Failed to deserialize file metadata");
                }

                using var aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = aesIV;

                using var outputStream = new MemoryStream();
                using (var cs = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cs.CopyTo(outputStream);
                }

                stopwatch.Stop();

                return (outputStream.ToArray(), metadata.OriginalFileName, metadata.ContentType, stopwatch.Elapsed.TotalMilliseconds);
            });
        }

        private bool ReadExactly(Stream stream, byte[] buffer, int count)
        {
            int bytesRead = 0;
            while (bytesRead < count)
            {
                int read = stream.Read(buffer, bytesRead, count - bytesRead);
                if (read == 0)
                {
                    return false;
                }
                bytesRead += read;
            }
            return true;
        }

        public async Task<(string encryptedText, double encryptionTime)> EncryptTextAsync(string plainText, RsaKeyDto publicKey)
        {
            var stopwatch = Stopwatch.StartNew();

            return await Task.Run(() =>
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(publicKey.PemKey);

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = rsa.Encrypt(plainBytes, RSAEncryptionPadding.OaepSHA256);
                string encryptedText = Convert.ToBase64String(encryptedBytes);

                stopwatch.Stop();

                return (encryptedText, stopwatch.Elapsed.TotalMilliseconds);
            });
        }

        public async Task<(string decryptedText, double decryptionTime)> DecryptTextAsync(string encryptedText, RsaKeyDto privateKey)
        {
            var stopwatch = Stopwatch.StartNew();

            return await Task.Run(() =>
            {
                using var rsa = RSA.Create();
                rsa.ImportFromPem(privateKey.PemKey);

                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = rsa.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA256);
                string decryptedText = Encoding.UTF8.GetString(decryptedBytes);

                stopwatch.Stop();

                return (decryptedText, stopwatch.Elapsed.TotalMilliseconds);
            });
        }
    }
}