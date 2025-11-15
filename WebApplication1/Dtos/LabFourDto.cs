using System.Text.Json.Serialization;

namespace WebApplication1.Dtos
{
    public class RsaKeyDto
    {
        public string PemKey { get; set; }
    }


    public class EncryptRequest
    {
        public IFormFile File { get; set; } = null!;
        public IFormFile? publicKeyFile { get; set; }
        public string? publicKeyPem { get; set; }
    }

    public class DecryptRequest
    {
        public IFormFile File { get; set; }
        public IFormFile? privateKeyFile { get; set; }
        public string? privateKeyPem { get; set; }
    }

    public class PublicRequest
    {
        public IFormFile Key { get; set; }
    }

    public class PrivateRequest
    {
        public IFormFile Key { get; set; }
    }

    public class EncryptionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? FileName { get; set; }
        public long? FileSize { get; set; }
        public double? ProcessingTimeMs { get; set; }
    }

    public class EncryptionTextResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? EncryptedText { get; set; }
        public double? ProcessingTimeMs { get; set; }
    }

    public class DecryptionTextResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string? DecryptedText { get; set; }
        public double? ProcessingTimeMs { get; set; }
    }
    public class FileMetadata
    {
        public string OriginalFileName { get; set; }
        public string ContentType { get; set; }
        public string EncryptedAt { get; set; }
    }

    public class EncryptTextRequest
    {
        public string Text { get; set; }
        public IFormFile? publicKeyFile { get; set; }
        public string? publicKeyPem { get; set; }
    }

    public class DecryptTextRequest
    {
        public string Text { get; set; }
        public IFormFile? privateKeyFile { get; set; }
        public string? privateKeyPem { get; set; }
    }
}
