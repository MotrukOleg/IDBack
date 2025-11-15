using WebApplication1.Dtos;

namespace WebApplication1.Interfaces.IRsaService
{
    public interface IRsaService
    {
        Task<(RsaKeyDto publicKey, RsaKeyDto privateKey)> GenerateKeysAsync(int keySize = 2048);
        Task SavePublicKeyAsync(RsaKeyDto key, string filePath);
        Task SavePrivateKeyAsync(RsaKeyDto privateKey, string filePath);
        Task<RsaKeyDto> LoadPublicKeyAsync(string filePath);
        Task<RsaKeyDto> LoadPrivateKeyAsync(string filePath);
        Task DeleteKeyAsync(string filePath);
        Task<(byte[] encryptedData, double encryptionTime)> EncryptFileAsync(
            Stream inputStream,
            RsaKeyDto publicKey,
            string originalFileName,
            string contentType);
        Task<(byte[] decryptedData, string originalFileName, string contentType, double decryptionTime)> DecryptFileAsync(
            Stream inputStream,
            RsaKeyDto privateKey);
        Task<(string encryptedText, double encryptionTime)> EncryptTextAsync(
            string plainText,
            RsaKeyDto publicKey);
        Task<(string decryptedText, double decryptionTime)> DecryptTextAsync(
            string encryptedText,
            RsaKeyDto privateKey);
    }
}
