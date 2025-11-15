using Microsoft.AspNetCore.Http;
using WebApplication1.Dtos;

namespace WebApplication1.Interfaces.IDssService;

public interface IDssService
{
    Task<KeyGenerationResponseDto> GenerateKeys(string? publicKeyFileName = null, string? privateKeyFileName = null);
    
    List<string> GetAvailableKeyFiles();
    Task<KeyExportResponseDto> DownloadKeyFromServer(string fileName);
    Task<KeySaveResponseDto> DeleteKeyFile(string fileName);
    
    Task<KeyImportResponseDto> LoadKeyFromServer(string fileName, bool isPrivateKey);
    KeyImportResponseDto ImportKey(KeyImportRequestDto request);
    Task<KeyImportResponseDto> ImportKeyFromFile(IFormFile file, bool isPrivateKey);
    
    string? GetPublicKey();
    string? GetPrivateKey();
    bool HasPrivateKey();
    bool HasPublicKey();
    
    SignatureResponseDto SignText(SignatureRequestDto request);
    Task<SignatureResponseDto> SignFile(IFormFile file);
    
    VerifyResponseDto VerifyText(VerifyRequestDto request);
    Task<VerifyResponseDto> VerifyFile(IFormFile file, string signatureHex);
}