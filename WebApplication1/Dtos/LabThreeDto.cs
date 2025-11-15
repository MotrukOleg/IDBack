using System.ComponentModel.DataAnnotations;
using System.Text;

namespace WebApplication1.Dtos
{
    public class EncryptRequestDto
    {
        [Required(ErrorMessage = "Парольна фраза обов'язкова")]
        [MinLength(1, ErrorMessage = "Парольна фраза не може бути порожньою")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Дані для шифрування обов'язкові")]
        public string Data { get; set; } = null!;

        [Range(64, 256, ErrorMessage = "Розмір ключа повинен бути 64, 128 або 256")]
        public int KeySize { get; set; } = 128;
    }

    public class DecryptRequestDto
    {
        [Required(ErrorMessage = "Парольна фраза обов'язкова")]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Зашифровані дані обов'язкові")]
        public string EncryptedData { get; set; } = null!;

        [Range(64, 256, ErrorMessage = "Розмір ключа повинен бути 64, 128 або 256")]
        public int KeySize { get; set; } = 128;
    }

    public class EncryptFileRequestDto
    {
        [Required]
        public IFormFile File { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Range(64, 256)]
        public int KeySize { get; set; } = 128;
    }

    public class DecryptFileRequestDto
    {
        [Required]
        public IFormFile EncryptedFile { get; set; } = null!;

        [Required]
        public string Password { get; set; } = null!;

        [Range(64, 256)]
        public int KeySize { get; set; } = 128;
    }

    public class EncryptedFileMetadataDto
    {
        public string OriginalFileName { get; set; } = null!;
        public string OriginalExtension { get; set; } = null!;
        public long OriginalSize { get; set; }
        public DateTime EncryptedAt { get; set; }
        public int KeySize { get; set; }

        public byte[] ToBytes()
        {
            var json = System.Text.Json.JsonSerializer.Serialize(this);
            return Encoding.UTF8.GetBytes(json);
        }

        public static EncryptedFileMetadataDto FromBytes(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            return System.Text.Json.JsonSerializer.Deserialize<EncryptedFileMetadataDto>(json)!;
        }
    }

    public class EncryptionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string EncryptedData { get; set; } = null!;
        public string Iv { get; set; } = null!;
        public int KeySize { get; set; }
    }

    public class DecryptionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string DecryptedData { get; set; } = null!;
    }

    public class FileEncryptionResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = null!;
        public string FileName { get; set; } = null!;
        public long OriginalSize { get; set; }
        public long EncryptedSize { get; set; }
    }
}
