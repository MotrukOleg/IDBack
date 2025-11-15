using System.Security.Cryptography;
using System.Text;
using WebApplication1.Dtos;
using WebApplication1.Interfaces.IDssService;

namespace WebApplication1.Services.DssService;

public class DssService : IDssService
{
    private DSA? _dsa;
    private string? _publicKeyPem;
    private string? _privateKeyPem;
    private readonly ILogger<DssService> _logger;
    private readonly string _keysDirectory;
    
    public DssService(ILogger<DssService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _keysDirectory = configuration["KeysDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Keys");
        
        if (!Directory.Exists(_keysDirectory))
        {
            Directory.CreateDirectory(_keysDirectory);
            _logger.LogInformation("Keys directory created: {Directory}", _keysDirectory);
        }
    }
    
    public string? GetPublicKey()
    {
        return _publicKeyPem;
    }

    public string? GetPrivateKey()
    {
        return _privateKeyPem;
    }

    public bool HasPrivateKey()
    {
        return !string.IsNullOrEmpty(_privateKeyPem);
    }

    public bool HasPublicKey()
    {
        return !string.IsNullOrEmpty(_publicKeyPem);
    }
    
    public async Task<KeyGenerationResponseDto> GenerateKeys(string? publicKeyFileName = null, string? privateKeyFileName = null)
    {
        try
        {
            _dsa = DSA.Create(2048);
            
            _publicKeyPem = _dsa.ExportSubjectPublicKeyInfoPem();
            _privateKeyPem = _dsa.ExportPkcs8PrivateKeyPem();
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            publicKeyFileName ??= $"public_key_{timestamp}.pem";
            privateKeyFileName ??= $"private_key_{timestamp}.pem";
            
            var publicKeyPath = Path.Combine(_keysDirectory, publicKeyFileName);
            var privateKeyPath = Path.Combine(_keysDirectory, privateKeyFileName);

            await File.WriteAllTextAsync(publicKeyPath, _publicKeyPem);
            await File.WriteAllTextAsync(privateKeyPath, _privateKeyPem);

            _logger.LogInformation("DSA keys successfully generated and saved: {PublicPath}, {PrivatePath}", 
                publicKeyPath, privateKeyPath);

            return new KeyGenerationResponseDto
            {
                Success = true,
                Message = "Keys successfully generated and saved",
                PublicKeyFileName = publicKeyFileName,
                PrivateKeyFileName = privateKeyFileName,
                Keys = new KeyPairDto
                {
                    PublicKey = _publicKeyPem,
                    PrivateKey = _privateKeyPem
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating keys");
            return new KeyGenerationResponseDto
            {
                Success = false,
                Message = $"Error generating keys: {ex.Message}"
            };
        }
    }
    
    public KeyImportResponseDto ImportKey(KeyImportRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.KeyPem))
            {
                return new KeyImportResponseDto
                {
                    Success = false,
                    Message = "KeyPem cannot be empty"
                };
            }

            if (request.IsPrivateKey)
            {
                _privateKeyPem = request.KeyPem;
                _dsa = DSA.Create();
                _dsa.ImportFromPem(_privateKeyPem);
                
                _publicKeyPem = _dsa.ExportSubjectPublicKeyInfoPem();
                
                _logger.LogInformation("Private key successfully imported from PEM");
            }
            else
            {
                _publicKeyPem = request.KeyPem;
                _logger.LogInformation("Public key successfully imported from PEM");
            }

            return new KeyImportResponseDto
            {
                Success = true,
                Message = $"{(request.IsPrivateKey ? "Private" : "Public")} key successfully imported"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing key");
            return new KeyImportResponseDto
            {
                Success = false,
                Message = $"Error importing key: {ex.Message}"
            };
        }
    }
    
    public async Task<KeyImportResponseDto> ImportKeyFromFile(IFormFile file, bool isPrivateKey)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return new KeyImportResponseDto
                {
                    Success = false,
                    Message = "File not selected or empty"
                };
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var keyPem = await reader.ReadToEndAsync();

            return ImportKey(new KeyImportRequestDto
            {
                KeyPem = keyPem,
                IsPrivateKey = isPrivateKey
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing key from file");
            return new KeyImportResponseDto
            {
                Success = false,
                Message = $"Error importing key from file: {ex.Message}"
            };
        }
    }
    
    public async Task<KeyImportResponseDto> LoadKeyFromServer(string fileName, bool isPrivateKey)
    {
        try
        {
            var filePath = Path.Combine(_keysDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                return new KeyImportResponseDto
                {
                    Success = false,
                    Message = $"File not found: {fileName}"
                };
            }

            var keyPem = await File.ReadAllTextAsync(filePath);
            
            var result = ImportKey(new KeyImportRequestDto
            {
                KeyPem = keyPem,
                IsPrivateKey = isPrivateKey
            });

            if (result.Success)
            {
                _logger.LogInformation("Key loaded from file: {FileName}", fileName);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading key from server");
            return new KeyImportResponseDto
            {
                Success = false,
                Message = $"Error loading key: {ex.Message}"
            };
        }
    }
    
    public async Task<KeyExportResponseDto> DownloadKeyFromServer(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_keysDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                return new KeyExportResponseDto
                {
                    Success = false,
                    Message = $"File not found: {fileName}"
                };
            }

            var keyContent = await File.ReadAllTextAsync(filePath);
            var bytes = Encoding.UTF8.GetBytes(keyContent);
            
            _logger.LogInformation("Key file prepared for download: {FileName}", fileName);

            return new KeyExportResponseDto
            {
                Success = true,
                Message = "File prepared for download",
                FileContent = bytes,
                FileName = fileName,
                ContentType = "application/x-pem-file"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading key file");
            return new KeyExportResponseDto
            {
                Success = false,
                Message = $"Error downloading file: {ex.Message}"
            };
        }
    }
    
    public List<string> GetAvailableKeyFiles()
    {
        try
        {
            if (!Directory.Exists(_keysDirectory))
            {
                return new List<string>();
            }

            var files = Directory.GetFiles(_keysDirectory, "*.pem")
                .Select(Path.GetFileName)
                .Where(f => f != null)
                .Cast<string>()
                .OrderByDescending(f => f)
                .ToList();

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving file list");
            return new List<string>();
        }
    }
    
    public async Task<KeySaveResponseDto> DeleteKeyFile(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_keysDirectory, fileName);
            
            if (!File.Exists(filePath))
            {
                return new KeySaveResponseDto
                {
                    Success = false,
                    Message = $"File not found: {fileName}"
                };
            }

            File.Delete(filePath);
            
            _logger.LogInformation("Key file deleted: {FileName}", fileName);

            return new KeySaveResponseDto
            {
                Success = true,
                Message = "File successfully deleted",
                FilePath = fileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting key file");
            return new KeySaveResponseDto
            {
                Success = false,
                Message = $"Error deleting file: {ex.Message}"
            };
        }
    }
    
    public SignatureResponseDto SignText(SignatureRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return new SignatureResponseDto
                {
                    Success = false,
                    Message = "Text cannot be empty"
                };
            }

            if (!HasPrivateKey() || _dsa == null)
            {
                return new SignatureResponseDto
                {
                    Success = false,
                    Message = "Private key not loaded. Generate or import keys first."
                };
            }

            byte[] data = Encoding.UTF8.GetBytes(request.Text);
            byte[] signature = _dsa.SignData(data, HashAlgorithmName.SHA256);
            string signatureHex = BitConverter.ToString(signature).Replace("-", "");

            _logger.LogInformation("Text successfully signed. Signature length: {Length} bytes", signature.Length);

            return new SignatureResponseDto
            {
                Success = true,
                Message = "Text successfully signed",
                SignatureHex = signatureHex,
                SignatureLength = signature.Length,
                Algorithm = "DSA",
                KeySize = _dsa.KeySize,
                OriginalText = request.Text
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating text signature");
            return new SignatureResponseDto
            {
                Success = false,
                Message = $"Error creating signature: {ex.Message}"
            };
        }
    }
    
    public async Task<SignatureResponseDto> SignFile(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return new SignatureResponseDto
                {
                    Success = false,
                    Message = "File not selected or empty"
                };
            }

            if (!HasPrivateKey() || _dsa == null)
            {
                return new SignatureResponseDto
                {
                    Success = false,
                    Message = "Private key not loaded. Generate or import keys first."
                };
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] fileData = memoryStream.ToArray();

            byte[] signature = _dsa.SignData(fileData, HashAlgorithmName.SHA256);
            string signatureHex = BitConverter.ToString(signature).Replace("-", "");

            _logger.LogInformation("File '{FileName}' successfully signed. Size: {Size} bytes, Signature length: {SigLength} bytes", 
                file.FileName, fileData.Length, signature.Length);

            return new SignatureResponseDto
            {
                Success = true,
                Message = "File successfully signed",
                SignatureHex = signatureHex,
                SignatureLength = signature.Length,
                Algorithm = "DSA",
                KeySize = _dsa.KeySize,
                OriginalFileName = file.FileName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating file signature");
            return new SignatureResponseDto
            {
                Success = false,
                Message = $"Error creating file signature: {ex.Message}"
            };
        }
    }
    
    public VerifyResponseDto VerifyText(VerifyRequestDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Text))
            {
                return new VerifyResponseDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Text cannot be empty"
                };
            }

            if (string.IsNullOrWhiteSpace(request.SignatureHex))
            {
                return new VerifyResponseDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Signature cannot be empty"
                };
            }

            if (!HasPublicKey())
            {
                return new VerifyResponseDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Public key not loaded"
                };
            }

            byte[] data = Encoding.UTF8.GetBytes(request.Text);
            byte[] signature = HexToBytes(request.SignatureHex);

            var dsaVerify = DSA.Create();
            dsaVerify.ImportFromPem(_publicKeyPem!);

            bool isValid = dsaVerify.VerifyData(data, signature, HashAlgorithmName.SHA256);

            _logger.LogInformation("Text signature verification: {Result}", isValid ? "VALID" : "INVALID");

            return new VerifyResponseDto
            {
                Success = true,
                IsValid = isValid,
                Message = isValid ? "Signature is valid" : "Signature is invalid",
                Details = isValid 
                    ? "Data has not been modified since signing" 
                    : "WARNING! Data has been modified or signature does not match"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying text signature");
            return new VerifyResponseDto
            {
                Success = false,
                IsValid = false,
                Message = $"Error verifying signature: {ex.Message}"
            };
        }
    }
    
    public async Task<VerifyResponseDto> VerifyFile(IFormFile file, string signatureHex)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return new VerifyResponseDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "File not selected or empty"
                };
            }

            if (string.IsNullOrWhiteSpace(signatureHex))
            {
                return new VerifyResponseDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Signature cannot be empty"
                };
            }

            if (!HasPublicKey())
            {
                return new VerifyResponseDto
                {
                    Success = false,
                    IsValid = false,
                    Message = "Public key not loaded"
                };
            }

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            byte[] fileData = memoryStream.ToArray();
            
            byte[] signature = HexToBytes(signatureHex);

            var dsaVerify = DSA.Create();
            dsaVerify.ImportFromPem(_publicKeyPem!);

            bool isValid = dsaVerify.VerifyData(fileData, signature, HashAlgorithmName.SHA256);

            _logger.LogInformation("File '{FileName}' signature verification: {Result}", 
                file.FileName, isValid ? "VALID" : "INVALID");

            return new VerifyResponseDto
            {
                Success = true,
                IsValid = isValid,
                Message = isValid ? "Signature is valid" : "Signature is invalid",
                Details = isValid 
                    ? $"File '{file.FileName}' has not been modified" 
                    : $"WARNING! File '{file.FileName}' has been modified or signature does not match"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying file signature");
            return new VerifyResponseDto
            {
                Success = false,
                IsValid = false,
                Message = $"Error verifying file signature: {ex.Message}"
            };
        }
    }
    
    private byte[] HexToBytes(string hex)
    {
        hex = hex.Replace(" ", "").Replace("-", "").Replace("\r", "").Replace("\n", "").Replace("\t", "");
        
        if (hex.Length % 2 != 0)
        {
            throw new ArgumentException($"Invalid HEX string length: {hex.Length} characters. Must be even.");
        }
        
        foreach (char c in hex)
        {
            if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
            {
                throw new ArgumentException($"Invalid character in HEX string: '{c}'");
            }
        }

        byte[] bytes = new byte[hex.Length / 2];

        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }

        return bytes;
    }
}