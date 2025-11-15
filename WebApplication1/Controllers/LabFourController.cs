using Microsoft.AspNetCore.Mvc;
using WebApplication1.Constants.ControllersConstants;
using WebApplication1.Dtos;
using WebApplication1.Interfaces.IRsaService;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabFourController : ControllerBase
    { 
        private readonly IRsaService _rsaService;
        private readonly ILogger<LabFourController> _logger;
        private readonly string _keysDirectory;

        public LabFourController(IRsaService rsaService, ILogger<LabFourController> logger, IConfiguration configuration)
        {
            _rsaService = rsaService;
            _logger = logger;
            _keysDirectory = configuration["RSA:KeysDirectory"] ?? Path.Combine(Directory.GetCurrentDirectory(), "Keys");

            if (!Directory.Exists(_keysDirectory))
            {
                Directory.CreateDirectory(_keysDirectory);
            }
        }

        [HttpPost("generate-keys")]
        public async Task<IActionResult> GenerateKeys([FromQuery] int keySize = 2048)
        {
            try
            {
                if (keySize != 1024 && keySize != 2048 && keySize != 4096)
                {
                    return BadRequest(new { message = "Key size must be 1024, 2048, or 4096 bits" });
                }

                var (publicKey, privateKey) = await _rsaService.GenerateKeysAsync(keySize);

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string publicKeyPath = Path.Combine(_keysDirectory, $"public_key_{timestamp}.pem");
                string privateKeyPath = Path.Combine(_keysDirectory, $"private_key_{timestamp}.pem");

                await _rsaService.SavePublicKeyAsync(publicKey, publicKeyPath);
                await _rsaService.SavePrivateKeyAsync(privateKey, privateKeyPath);

                return Ok(new
                {
                    success = true,
                    message = "RSA key pair generated successfully",
                    publicKey = new
                    {
                        pemKey = publicKey.PemKey,
                    },
                    privateKeyPath = Path.GetFileName(privateKeyPath),
                    publicKeyPath = Path.GetFileName(publicKeyPath)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating RSA keys");
                return StatusCode(500, new { message = "Error generating keys", error = ex.Message });
            }
        }

        [HttpGet("public-key/{filename}")]
        public async Task<IActionResult> GetPublicKey(string filename)
        {
            try
            {
                string filePath = Path.GetFullPath(_keysDirectory + filename);
                if (!filePath.StartsWith(_keysDirectory))
                {
                    return BadRequest(new { message = LabFourControllerConstants.EntyIsOutsideOfTheTarget });
                }
                else if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = LabFourControllerConstants.PublicKeyNotFount });
                }

                var publicKey = await _rsaService.LoadPublicKeyAsync(filePath);
                return Ok(new
                {
                    pemKey = publicKey.PemKey,
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LabFourControllerConstants.ErrorLoadingPublic);
                return StatusCode(500, new { message = LabFourControllerConstants.ErrorLoadingPublic, error = ex.Message });
            }
        }

        [HttpGet("download-public-key/{filename}")]
        public async Task<IActionResult> DownloadPublicKey(string filename)
        {
            try
            {
                string filePath = Path.Combine(_keysDirectory, filename);
                if (!filePath.StartsWith(_keysDirectory))
                {
                    return BadRequest(new { message = LabFourControllerConstants.EntyIsOutsideOfTheTarget });
                }
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = LabFourControllerConstants.PublicKeyNotFount });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/x-pem-file", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LabFourControllerConstants.ErrorLoadingPublic);
                return StatusCode(500, new { message = LabFourControllerConstants.ErrorLoadingPublic, error = ex.Message });
            }
        }

        [HttpGet("download-private-key/{filename}")]
        public async Task<IActionResult> DownloadPrivateKey(string filename)
        {
            try
            {
                string filePath = Path.Combine(_keysDirectory, filename);
                if (!filePath.StartsWith(_keysDirectory))
                {
                    return BadRequest(new { message = LabFourControllerConstants.EntyIsOutsideOfTheTarget });
                }
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = LabFourControllerConstants.PublicKeyNotFount });
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/x-pem-file", filename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LabFourControllerConstants.ErrorLoadingPublic);
                return StatusCode(500, new { message = LabFourControllerConstants.ErrorLoadingPublic, error = ex.Message });
            }
        }

        [HttpGet("keys")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult ListKeys()
        {
            try
            {
                var publicKeys = Directory.GetFiles(_keysDirectory, "public_key_*.pem")
                    .Select(Path.GetFileName)
                    .ToList();

                var privateKeys = Directory.GetFiles(_keysDirectory, "private_key_*.pem")
                    .Select(Path.GetFileName)
                    .ToList();

                return Ok(new
                {
                    publicKeys,
                    privateKeys
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing keys");
                return StatusCode(500, new { message = "Error listing keys", error = ex.Message });
            }
        }

        [HttpPost("encrypt-file")]
        public async Task<IActionResult> EncryptFile([FromForm] EncryptRequest file)
        {
            try
            {
                if (file == null || file.File.Length == 0)
                {
                    return BadRequest(new { message = "File is required" });
                }

                RsaKeyDto publicKey;

                if (file.publicKeyFile != null && file.publicKeyFile.Length > 0)
                {
                    using var keyStream = file.publicKeyFile.OpenReadStream();
                    using var reader = new StreamReader(keyStream);
                    var pemContent = await reader.ReadToEndAsync();

                    publicKey = ParsePemFile(pemContent);
                }
                else if (!string.IsNullOrEmpty(file.publicKeyPem))
                {
                    publicKey = ParsePemFile(file.publicKeyPem);
                }
                else
                {
                    return BadRequest(new { message = "Either publicKeyFilename or publicKeyFile must be provided" });
                }

                using var inputStream = file.File.OpenReadStream();
                var (encryptedData, _) = await _rsaService.EncryptFileAsync(
                    inputStream,
                    publicKey,
                    file.File.FileName,
                    file.File.ContentType);
                

                return File(encryptedData, "application/octet-stream", $"{Path.GetFileNameWithoutExtension(file.File.FileName)}_encrypted.dat");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, LabFourControllerConstants.ErrorLoadingPublic);
                return StatusCode(500, new { message = LabFourControllerConstants.ErrorLoadingPublic, error = ex.Message });
            }
        }

        [HttpPost("decrypt-file")]
        public async Task<IActionResult> DecryptFile([FromForm] DecryptRequest file)
        {
            try
            {
                if (file == null || file.File.Length == 0)
                {
                    return BadRequest(new { message = "File is required" });
                }

                RsaKeyDto privateKey;

                if (file.privateKeyFile != null && file.privateKeyFile.Length > 0)
                {
                    using var keyStream = file.privateKeyFile.OpenReadStream();
                    using var reader = new StreamReader(keyStream);
                    var pemContent = await reader.ReadToEndAsync();

                    privateKey = ParsePemFile(pemContent);
                }
                else if (!string.IsNullOrEmpty(file.privateKeyPem))
                {
                    privateKey = ParsePemFile(file.privateKeyPem);
                }
                else
                {
                    return BadRequest(new { message = "Either privateKeyFilename or privateKeyFile must be provided" });
                }

                using var inputStream = file.File.OpenReadStream();
                var (decryptedData, originalFileName, contentType, processingTime) = await _rsaService.DecryptFileAsync(inputStream, privateKey);
                

                return File(decryptedData, contentType, originalFileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error decrypting file", error = ex.Message });
            }
        }

        [HttpPost("encrypt-text")]
        public async Task<IActionResult> EncryptText([FromForm] EncryptTextRequest request)
        {
            try
            {
                if (request.Text.Length == 0)
                {
                    return BadRequest(new { message = "Text is required" });
                }

                RsaKeyDto publicKey;

                if (request.publicKeyFile != null && request.publicKeyFile.Length > 0)
                {
                    using var keyStream = request.publicKeyFile.OpenReadStream();
                    using var reader = new StreamReader(keyStream);
                    var pemContent = await reader.ReadToEndAsync();

                    publicKey = ParsePemFile(pemContent);
                }
                else if (!string.IsNullOrEmpty(request.publicKeyPem))
                {
                    publicKey = ParsePemFile(request.publicKeyPem);
                }
                else
                {
                    return BadRequest(new { message = "Either publicKey or publicKeyFile must be provided" });
                }

                var (encryptedData, processingTime) = await _rsaService.EncryptTextAsync(
                    request.Text,
                    publicKey);

                _logger.LogInformation($"Text encrypted in {processingTime:F2} ms");

                return Ok(new EncryptionTextResponse
                {
                    Success = true,
                    Message = "Text encrypted successfully",
                    EncryptedText = encryptedData,
                    ProcessingTimeMs = processingTime
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error encrypting file", error = ex.Message });
            }
        }

        [HttpPost("decrypt-text")]
        public async Task<IActionResult> DecryptText([FromForm] DecryptTextRequest request)
        {
            try
            {
                if (request.Text.Length == 0)
                {
                    return BadRequest(new { message = "Text is required" });
                }

                RsaKeyDto privateKey;

                if (request.privateKeyFile != null && request.privateKeyFile.Length > 0)
                {
                    using var keyStream = request.privateKeyFile.OpenReadStream();
                    using var reader = new StreamReader(keyStream);
                    var pemContent = await reader.ReadToEndAsync();

                    privateKey = ParsePemFile(pemContent);
                }
                else if (!string.IsNullOrEmpty(request.privateKeyPem))
                {
                    privateKey = ParsePemFile(request.privateKeyPem);
                }
                else
                {
                    return BadRequest(new { message = "Either privateKey or privateKeyFile must be provided" });
                }

                var (decryptedData, processingTime) = await _rsaService.DecryptTextAsync(request.Text, privateKey);

                _logger.LogInformation($"Text decrypted in {processingTime:F2} ms.");

                return Ok(new DecryptionTextResponse {
                    Success = true,
                    Message = "Text decrypted successfully",
                    DecryptedText = decryptedData,
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error decrypting file", error = ex.Message });
            }
        }

        [HttpDelete("delete-key/{filename}")]
        public async Task<IActionResult> DeleteKey(string filename)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filename) ||
                    filename.Contains("..") ||
                    filename.Contains("/") ||
                    filename.Contains("\\"))
                {
                    return BadRequest(new { message = "Invalid filename" });
                }

                string filePath = Path.Combine(_keysDirectory, filename);
                if (!filePath.StartsWith(_keysDirectory))
                {
                    return BadRequest(new { message = LabFourControllerConstants.EntyIsOutsideOfTheTarget });
                }
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound(new { message = LabFourControllerConstants.PublicKeyNotFount });
                }

                await _rsaService.DeleteKeyAsync(filePath);

                return Ok(new
                {
                    success = true,
                    message = $"Key file '{filename}' deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting key file", error = ex.Message });
            }
        }

        private RsaKeyDto ParsePemFile(string pemContent)
        {
            var lines = pemContent.Split('\n');

            if (lines[0].StartsWith("# Created at:"))
            {
                pemContent = string.Join('\n', lines.Skip(1)).Trim();
            }

            return new RsaKeyDto
            {
                PemKey = pemContent
            };
        }
    }
}