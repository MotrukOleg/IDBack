namespace WebApplication1.Dtos
{
    public class FileRequestDto
    {
        public IFormFile? File { get; set; }
    }
    public class GenerateKeysRequestDto
    {
        public string? PublicKeyFileName { get; set; }
        public string? PrivateKeyFileName { get; set; }
    }
    
    public class SignatureRequestDto
    {
        public string? Text { get; set; }
    }
    
    public class SignatureResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? SignatureHex { get; set; }
        public int? SignatureLength { get; set; }
        public string? Algorithm { get; set; }
        public int? KeySize { get; set; }
        public string? OriginalText { get; set; }
        public string? OriginalFileName { get; set; }
    }
    public class VerifyRequestDto
    {
        public string? Text { get; set; }
        public string SignatureHex { get; set; } = string.Empty;
    }
    
    public class VerifyResponseDto
    {
        public bool Success { get; set; }
        public bool IsValid { get; set; }
        public string? Message { get; set; }
        public string? Details { get; set; }
    }
    
    public class KeyPairDto
    {
        public string PublicKey { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
    }
    
    public class KeyGenerationResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? PublicKeyFileName { get; set; }
        public string? PrivateKeyFileName { get; set; }
        public KeyPairDto? Keys { get; set; }
    }
    
    public class KeyImportRequestDto
    {
        public string KeyPem { get; set; } = string.Empty;
        public bool IsPrivateKey { get; set; }
    }
    
    public class KeyImportResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }
    
    public class KeyExportResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public byte[]? FileContent { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
    }
    
    public class KeySaveResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? FilePath { get; set; }
    }
    
    public class LoadKeyRequestDto
    {
        public string FileName { get; set; } = string.Empty;
        public bool IsPrivateKey { get; set; }
    }
}