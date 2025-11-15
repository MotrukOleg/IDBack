using WebApplication1.Dtos;

namespace WebApplication1.Interfaces.IRc5Service
{
    public interface IRc5Service
    {
 
        (byte[] encryptedData, byte[] iv) EncryptText(string data, string password, int keySize = 128);

        string DecryptText(byte[] encryptedData, string password, int keySize = 128);

        byte[] EncryptBytes(byte[] data, string password, int keySize = 128);

        byte[] DecryptBytes(byte[] encryptedData, string password, int keySize = 128);

        byte[] EncryptBytesWithMetadata(byte[] data, string password, string fileName, int keySize = 128);

        /// <summary>
        (byte[] data, EncryptedFileMetadataDto metadata) DecryptBytesWithMetadata(byte[] encryptedData, string password, int keySize = 128);

        byte[] DeriveKey(string password, int keySize);
    }
}
