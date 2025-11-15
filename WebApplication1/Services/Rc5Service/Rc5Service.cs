using System.Text;
using WebApplication1.Dtos;
using WebApplication1.Interfaces.IMD5Service;
using WebApplication1.Interfaces.IPseudoGeneratorService;
using WebApplication1.Interfaces.IRc5Service;

namespace WebApplication1.Services.RC5Service
{
    public class Rc5Service : IRc5Service
    {
        private readonly ILogger<Rc5Service> _logger;
        private readonly IMd5Service _md5Service;

        public Rc5Service(ILogger<Rc5Service> logger, IMd5Service md5Service, IPseudoGeneratorService pseudoGen)
        {
            _logger = logger;
            _md5Service = md5Service;
        }

        public (byte[] encryptedData, byte[] iv) EncryptText(string data, string password, int keySize = 128)
        {
            try
            {

                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                byte[] key = DeriveKey(password, keySize);

                byte[] iv = Generate();

                var rc5 = new Rc5Cipher(key);
                byte[] encrypted = rc5.EncryptCBC(dataBytes, iv);

                _logger.LogInformation("Text successfully encrypted");
                return (encrypted, iv);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while encrypting text");
                throw;
            }
        }

        public string DecryptText(byte[] encryptedData, string password, int keySize = 128)
        {
            try
            {

                byte[] key = DeriveKey(password, keySize);
                var rc5 = new Rc5Cipher(key);

                byte[] decrypted = rc5.DecryptCBC(encryptedData);
                string result = Encoding.UTF8.GetString(decrypted);

                _logger.LogInformation("Текст успішно дешифровано");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while decrypting text");
                throw;
            }
        }

        public byte[] EncryptBytesWithMetadata(byte[] data, string password, string fileName, int keySize = 128)
        {

            var metadata = new EncryptedFileMetadataDto
            {
                OriginalFileName = Path.GetFileNameWithoutExtension(fileName),
                OriginalExtension = Path.GetExtension(fileName),
                OriginalSize = data.Length,
                EncryptedAt = DateTime.UtcNow,
                KeySize = keySize
            };

            byte[] metadataBytes = metadata.ToBytes();
            byte[] key = DeriveKey(password, keySize);
            byte[] metadataIV = Generate();
            var rc5Metadata = new Rc5Cipher(key);
            byte[] encryptedMetadata = rc5Metadata.EncryptCBC(metadataBytes, metadataIV);

            byte[] dataIV = Generate();
            var rc5Data = new Rc5Cipher(key);
            byte[] encryptedData = rc5Data.EncryptCBC(data, dataIV);

            byte[] result = new byte[4 + encryptedMetadata.Length + encryptedData.Length];

            Array.Copy(BitConverter.GetBytes(encryptedMetadata.Length), 0, result, 0, 4);
            Array.Copy(encryptedMetadata, 0, result, 4, encryptedMetadata.Length);
            Array.Copy(encryptedData, 0, result, 4 + encryptedMetadata.Length, encryptedData.Length);

            _logger.LogInformation("Metadata file successfully encrypted");
            return result;
        }

        public byte[] EncryptBytes(byte[] data, string password, int keySize = 128)
        {
            try
            {

                byte[] key = DeriveKey(password, keySize);
                byte[] iv = Generate();

                var rc5 = new Rc5Cipher(key);
                byte[] encrypted = rc5.EncryptCBC(data, iv);
                
                return encrypted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error encrypting file");
                throw;
            }
        }

        public (byte[] data, EncryptedFileMetadataDto metadata) DecryptBytesWithMetadata(byte[] encryptedData, string password, int keySize = 128)
        {
            try
            {
                _logger.LogInformation("Decryption with metadata");

                int metadataLength = BitConverter.ToInt32(encryptedData, 0);

                byte[] encryptedMetadata = new byte[metadataLength];
                Array.Copy(encryptedData, 4, encryptedMetadata, 0, metadataLength);

                int dataLength = encryptedData.Length - 4 - metadataLength;
                byte[] encryptedFileData = new byte[dataLength];
                Array.Copy(encryptedData, 4 + metadataLength, encryptedFileData, 0, dataLength);

                byte[] key = DeriveKey(password, keySize);
                var rc5Metadata = new Rc5Cipher(key);
                byte[] decryptedMetadataBytes = rc5Metadata.DecryptCBC(encryptedMetadata);
                var metadata = EncryptedFileMetadataDto.FromBytes(decryptedMetadataBytes);

                var rc5Data = new Rc5Cipher(key);
                byte[] decryptedData = rc5Data.DecryptCBC(encryptedFileData);
                
                return (decryptedData, metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while decrypting with metadata");
                throw;
            }
        }

        public byte[] DecryptBytes(byte[] encryptedData, string password, int keySize = 128)
        {
            try
            {

                byte[] key = DeriveKey(password, keySize);
                var rc5 = new Rc5Cipher(key);

                byte[] decrypted = rc5.DecryptCBC(encryptedData);
                
                return decrypted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while decrypting file");
                throw;
            }
        }

        public byte[] DeriveKey(string password, int keySize)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);

            return keySize switch
            {
                64 => DeriveKey64(passwordBytes),
                128 => DeriveKey128(passwordBytes),
                256 => DeriveKey256(passwordBytes),
                _ => throw new ArgumentException($"Unsupported key size: {keySize}")
            };
        }

        private byte[] DeriveKey64(byte[] passwordBytes)
        {
            byte[] hash = _md5Service.ComputeMD5HashBytes(passwordBytes);
            byte[] key = new byte[8];
            Array.Copy(hash, 0, key, 0, 8);
            return key;
        }

        private byte[] DeriveKey128(byte[] passwordBytes)
        {
            return _md5Service.ComputeMD5HashBytes(passwordBytes);
        }

        private byte[] DeriveKey256(byte[] passwordBytes)
        {
            byte[] hash1 = _md5Service.ComputeMD5HashBytes(passwordBytes);
            byte[] hash2 = _md5Service.ComputeMD5HashBytes(hash1);

            byte[] key = new byte[32];
            Array.Copy(hash2, 0, key, 0, 16);
            Array.Copy(hash1, 0, key, 16, 16);
            return key;
        }

        private static byte[] Generate()
        {
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            byte[] iv = new byte[8];
            rng.GetBytes(iv);
            return iv;
        }
    }
    internal class Rc5Cipher
    {
        private const int W = 32;
        private const int R = 20;
        private const int BlockSize = 2*(W / 8);

        private uint[] _s;
        private readonly int _keySize;

        public Rc5Cipher(byte[] key)
        {
            _keySize = key.Length;
            if (_keySize != 8 && _keySize != 16 && _keySize != 32)
                throw new ArgumentException("The key must be 8, 16 or 32 bytes.");

            KeyExpansion(key);
        }

        private void KeyExpansion(byte[] key)
        {
            uint P = 0xB7E15163;
            uint Q = 0x9E3779B9;
            int T = 2 * (R + 1);
            int C = Math.Max(1, _keySize / 4);

            uint[] L = new uint[C];
            for (int i = 0; i < _keySize; i++)
            {
                L[i / 4] = (L[i / 4] << 8) + key[i];
            }

            _s = new uint[T];
            _s[0] = P;
            for (int i = 1; i < T; i++)
            {
                _s[i] = _s[i - 1] + Q;
            }

            uint A = 0, BB = 0;
            int ii = 0, jj = 0;
            int v = 3 * Math.Max(T, C);

            for (int k = 0; k < v; k++)
            {
                A = _s[ii] = RotateLeft((_s[ii] + A + BB), 3);
                BB = L[jj] = RotateLeft((L[jj] + A + BB), (int)(A + BB));
                ii = (ii + 1) % T;
                jj = (jj + 1) % C;
            }
        }

        private static uint RotateLeft(uint value, int count)
        {
            count &= (W - 1);
            return (value << count) | (value >> (W - count));
        }

        private static uint RotateRight(uint value, int count)
        {
            count &= (W - 1);
            return (value >> count) | (value << (W - count));
        }

        public byte[] EncryptBlockECB(byte[] block)
        {
            if (block.Length != BlockSize)
                throw new ArgumentException($"The block must be {BlockSize} bytes");

            uint A = BitConverter.ToUInt32(block, 0);
            uint BB = BitConverter.ToUInt32(block, 4);

            A += _s[0];
            BB += _s[1];

            for (int i = 1; i <= R; i++)
            {
                A = RotateLeft(A ^ BB, (int)(BB & 0x1F)) + _s[2 * i];
                BB = RotateLeft(BB ^ A, (int)(A & 0x1F)) + _s[2 * i + 1];
            }

            byte[] result = new byte[BlockSize];
            Array.Copy(BitConverter.GetBytes(A), 0, result, 0, 4);
            Array.Copy(BitConverter.GetBytes(BB), 0, result, 4, 4);

            return result;
        }

        public byte[] DecryptBlockECB(byte[] block)
        {
            if (block.Length != BlockSize)
                throw new ArgumentException($"The block must be {BlockSize} bytes");

            uint A = BitConverter.ToUInt32(block, 0);
            uint BB = BitConverter.ToUInt32(block, 4);

            for (int i = R; i >= 1; i--)
            {
                BB = RotateRight(BB - _s[2 * i + 1], (int)(A & 0x1F)) ^ A;
                A = RotateRight(A - _s[2 * i], (int)(BB & 0x1F)) ^ BB;
            }

            BB -= _s[1];
            A -= _s[0];

            byte[] result = new byte[BlockSize];
            Array.Copy(BitConverter.GetBytes(A), 0, result, 0, 4);
            Array.Copy(BitConverter.GetBytes(BB), 0, result, 4, 4);

            return result;
        }

        public byte[] EncryptCBC(byte[] data, byte[] iv)
        {
            if (iv.Length != BlockSize)
                throw new ArgumentException($"IV must be {BlockSize} bytes");

            int paddingSize = BlockSize - (data.Length % BlockSize);
            byte[] paddedData = new byte[data.Length + paddingSize];
            Array.Copy(data, paddedData, data.Length);
            for (int i = data.Length; i < paddedData.Length; i++)
            {
                paddedData[i] = (byte)paddingSize;
            }

            byte[] encryptedIV = EncryptBlockECB(iv);
            int blockCount = paddedData.Length / BlockSize;
            byte[] result = new byte[BlockSize + paddedData.Length];

            Array.Copy(encryptedIV, 0, result, 0, BlockSize);
            byte[] previousBlock = iv;

            for (int i = 0; i < blockCount; i++)
            {
                byte[] block = new byte[BlockSize];
                Array.Copy(paddedData, i * BlockSize, block, 0, BlockSize);

                for (int j = 0; j < BlockSize; j++)
                {
                    block[j] ^= previousBlock[j];
                }

                byte[] encryptedBlock = EncryptBlockECB(block);
                Array.Copy(encryptedBlock, 0, result, BlockSize + i * BlockSize, BlockSize);
                previousBlock = encryptedBlock;
            }

            return result;
        }

        public byte[] DecryptCBC(byte[] encryptedData)
        {
            if (encryptedData.Length < BlockSize * 2)
                throw new ArgumentException("Encrypted data is too short");

            if (encryptedData.Length % BlockSize != 0)
                throw new ArgumentException("The length of the encrypted data must be a multiple of the block size");

            byte[] encryptedIV = new byte[BlockSize];
            Array.Copy(encryptedData, 0, encryptedIV, 0, BlockSize);
            byte[] iv = DecryptBlockECB(encryptedIV);

            int dataLength = encryptedData.Length - BlockSize;
            int blockCount = dataLength / BlockSize;
            byte[] decryptedData = new byte[dataLength];
            byte[] previousBlock = iv;

            for (int i = 0; i < blockCount; i++)
            {
                byte[] encryptedBlock = new byte[BlockSize];
                Array.Copy(encryptedData, BlockSize + i * BlockSize, encryptedBlock, 0, BlockSize);

                byte[] decryptedBlock = DecryptBlockECB(encryptedBlock);

                for (int j = 0; j < BlockSize; j++)
                {
                    decryptedBlock[j] ^= previousBlock[j];
                }

                Array.Copy(decryptedBlock, 0, decryptedData, i * BlockSize, BlockSize);
                previousBlock = encryptedBlock;
            }

            int paddingSize = decryptedData[decryptedData.Length - 1];
            if (paddingSize < 1 || paddingSize > BlockSize)
                throw new ArgumentException("Incorrect padding");

            byte[] result = new byte[decryptedData.Length - paddingSize];
            Array.Copy(decryptedData, result, result.Length);

            return result;
        }
    }
}