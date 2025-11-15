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

                _logger.LogInformation($"Generated RSA key pair with size {keySize} bits");
                return (publicKey, privateKey);
            });
        }

        public async Task SavePublicKeyAsync(RsaKeyDto key, string filePath)
        {
            string pemFilePath = Path.ChangeExtension(filePath, ".pem");
            var content = new StringBuilder();
            content.Append(key.PemKey);

            await File.WriteAllTextAsync(pemFilePath, content.ToString());
            _logger.LogInformation($"Public key saved to {pemFilePath}");
        }

        public async Task SavePrivateKeyAsync(RsaKeyDto privateKey, string filePath)
        {
            string pemFilePath = Path.ChangeExtension(filePath, ".pem");
            var content = new StringBuilder();
            content.Append(privateKey.PemKey);

            await File.WriteAllTextAsync(pemFilePath, content.ToString());
            _logger.LogWarning($"Private key saved to {pemFilePath} - Keep this file secure!");
        }

        public async Task<RsaKeyDto> LoadPublicKeyAsync(string filePath)
        {
            string content = await File.ReadAllTextAsync(filePath);

            string createdAt = "Unknown";
            var lines = content.Split('\n');
            if (lines[0].StartsWith("# Created at:"))
            {
                createdAt = lines[0].Replace("# Created at:", "").Trim();
                content = string.Join('\n', lines.Skip(1));
            }

            var key = new RsaKeyDto
            {
                PemKey = content.Trim()
            };

            _logger.LogInformation($"Public key loaded from {filePath}");
            return key;
        }

        public async Task<RsaKeyDto> LoadPrivateKeyAsync(string filePath)
        {
            string content = await File.ReadAllTextAsync(filePath);

            string createdAt = "Unknown";
            var lines = content.Split('\n');
            if (lines[0].StartsWith("# Created at:"))
            {
                createdAt = lines[0].Replace("# Created at:", "").Trim();
                content = string.Join('\n', lines.Skip(1));
            }

            var key = new RsaKeyDto
            {
                PemKey = content.Trim()
            };

            _logger.LogInformation($"Private key loaded from {filePath}");
            return key;
        }

        public async Task DeleteKeyAsync(string filePath)
        {
            await Task.Run(() =>
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning($"Key file not found: {filePath}");
                    throw new FileNotFoundException($"Key file not found: {filePath}");
                }

                try
                {
                    File.Delete(filePath);
                    _logger.LogInformation($"Key file deleted successfully: {filePath}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error deleting key file: {filePath}");
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
                _logger.LogInformation($"File '{originalFileName}' encrypted in {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

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

                inputStream.Read(keyLengthBytes, 0, 4);
                inputStream.Read(ivLengthBytes, 0, 4);
                inputStream.Read(metadataLengthBytes, 0, 4);

                int keyLength = BitConverter.ToInt32(keyLengthBytes, 0);
                int ivLength = BitConverter.ToInt32(ivLengthBytes, 0);
                int metadataLength = BitConverter.ToInt32(metadataLengthBytes, 0);

                byte[] encryptedAesKey = new byte[keyLength];
                byte[] encryptedAesIV = new byte[ivLength];
                byte[] metadataBytes = new byte[metadataLength];

                inputStream.Read(encryptedAesKey, 0, keyLength);
                inputStream.Read(encryptedAesIV, 0, ivLength);
                inputStream.Read(metadataBytes, 0, metadataLength);

                byte[] aesKey = rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);
                byte[] aesIV = rsa.Decrypt(encryptedAesIV, RSAEncryptionPadding.OaepSHA256);

                string metadataJson = Encoding.UTF8.GetString(metadataBytes);
                var metadata = System.Text.Json.JsonSerializer.Deserialize<FileMetadata>(metadataJson);

                using var aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = aesIV;

                using var outputStream = new MemoryStream();
                using (var cs = new CryptoStream(inputStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
                {
                    cs.CopyTo(outputStream);
                }

                stopwatch.Stop();
                _logger.LogInformation($"File decrypted in {stopwatch.Elapsed.TotalMilliseconds:F2} ms. Original: {metadata.OriginalFileName}");

                return (outputStream.ToArray(), metadata.OriginalFileName, metadata.ContentType, stopwatch.Elapsed.TotalMilliseconds);
            });
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
                _logger.LogInformation($"Text encrypted in {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

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
                _logger.LogInformation($"Text decrypted in {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

                return (decryptedText, stopwatch.Elapsed.TotalMilliseconds);
            });
        }
    }
}
