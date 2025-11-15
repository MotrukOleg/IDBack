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
            return await ExecuteAsync(async () =>
            {
                if (keySize != 1024 && keySize != 2048 && keySize != 4096)
                {
                    throw new ArgumentException("Key size must be 1024, 2048, or 4096 bits");
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
                    publicKey = new { pemKey = publicKey.PemKey },
                    privateKeyPath = Path.GetFileName(privateKeyPath),
                    publicKeyPath = Path.GetFileName(publicKeyPath)
                });
            }, "Error generating RSA keys");
        }

        [HttpGet("public-key/{filename}")]
        public async Task<IActionResult> GetPublicKey(string filename)
        {
            return await ExecuteAsync(async () =>
            {
                string filePath = ValidateAndGetFilePath(filename);
                var publicKey = await _rsaService.LoadPublicKeyAsync(filePath);
                return Ok(new { pemKey = publicKey.PemKey });
            }, LabFourControllerConstants.ErrorLoadingPublic);
        }

        [HttpGet("download-public-key/{filename}")]
        public async Task<IActionResult> DownloadPublicKey(string filename)
        {
            return await ExecuteAsync(async () =>
            {
                string filePath = ValidateAndGetFilePath(filename);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/x-pem-file", filename);
            }, LabFourControllerConstants.ErrorLoadingPublic);
        }

        [HttpGet("download-private-key/{filename}")]
        public async Task<IActionResult> DownloadPrivateKey(string filename)
        {
            return await ExecuteAsync(async () =>
            {
                string filePath = ValidateAndGetFilePath(filename);
                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                return File(fileBytes, "application/x-pem-file", filename);
            }, LabFourControllerConstants.ErrorLoadingPublic);
        }

        [HttpGet("keys")]
        [ProducesResponseType(typeof(string), 200)]
        public IActionResult ListKeys()
        {
            return Execute(() =>
            {
                var publicKeys = Directory.GetFiles(_keysDirectory, "public_key_*.pem")
                    .Select(Path.GetFileName)
                    .ToList();

                var privateKeys = Directory.GetFiles(_keysDirectory, "private_key_*.pem")
                    .Select(Path.GetFileName)
                    .ToList();

                return Ok(new { publicKeys, privateKeys });
            }, "Error listing keys");
        }

        [HttpPost("encrypt-file")]
        public async Task<IActionResult> EncryptFile([FromForm] EncryptRequest file)
        {
            return await ExecuteAsync(async () =>
            {
                ValidateFile(file?.File);
                RsaKeyDto publicKey = await ExtractKeyAsync(file?.publicKeyFile, file?.publicKeyPem);

                using var inputStream = file!.File.OpenReadStream();
                var (encryptedData, _) = await _rsaService.EncryptFileAsync(
                    inputStream, publicKey, file.File.FileName, file.File.ContentType);

                return File(encryptedData, "application/octet-stream",
                    $"{Path.GetFileNameWithoutExtension(file.File.FileName)}_encrypted.dat");
            }, LabFourControllerConstants.ErrorLoadingPublic);
        }

        [HttpPost("decrypt-file")]
        public async Task<IActionResult> DecryptFile([FromForm] DecryptRequest file)
        {
            return await ExecuteAsync(async () =>
            {
                ValidateFile(file?.File);
                RsaKeyDto privateKey = await ExtractKeyAsync(file!.privateKeyFile, file.privateKeyPem);

                using var inputStream = file.File.OpenReadStream();
                var (decryptedData, originalFileName, contentType, _) = await _rsaService.DecryptFileAsync(inputStream, privateKey);

                return File(decryptedData, contentType, originalFileName);
            }, LabFourControllerConstants.ErrorDecryptingFile);
        }

        [HttpPost("encrypt-text")]
        public async Task<IActionResult> EncryptText([FromForm] EncryptTextRequest request)
        {
            return await ExecuteAsync(async () =>
            {
                ValidateText(request?.Text);
                RsaKeyDto publicKey = await ExtractKeyAsync(request!.publicKeyFile, request.publicKeyPem);

                var (encryptedData, processingTime) = await _rsaService.EncryptTextAsync(request.Text, publicKey);

                return Ok(new EncryptionTextResponse
                {
                    Success = true,
                    Message = "Text encrypted successfully",
                    EncryptedText = encryptedData,
                    ProcessingTimeMs = processingTime
                });
            }, LabFourControllerConstants.ErrorEncryptingFile);
        }

        [HttpPost("decrypt-text")]
        public async Task<IActionResult> DecryptText([FromForm] DecryptTextRequest request)
        {
            return await ExecuteAsync(async () =>
            {
                ValidateText(request?.Text);
                RsaKeyDto privateKey = await ExtractKeyAsync(request!.privateKeyFile, request.privateKeyPem);

                var (decryptedData, _) = await _rsaService.DecryptTextAsync(request.Text, privateKey);

                return Ok(new DecryptionTextResponse
                {
                    Success = true,
                    Message = "Text decrypted successfully",
                    DecryptedText = decryptedData
                });
            }, LabFourControllerConstants.ErrorDecryptingFile);
        }

        [HttpDelete("delete-key/{filename}")]
        public async Task<IActionResult> DeleteKey(string filename)
        {
            return await ExecuteAsync(async () =>
            {
                string filePath = ValidateAndGetFilePath(filename);
                await _rsaService.DeleteKeyAsync(filePath);

                return Ok(new
                {
                    success = true,
                    message = "Key file successfully deleted",
                    filename = filename
                });
            }, "Error deleting key file");
        }

        private string ValidateAndGetFilePath(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                throw new ArgumentException("Filename cannot be empty");
            }

            if (filename.Contains("..") || filename.Contains("/") || filename.Contains("\\") || filename.Contains(":"))
            {
                throw new ArgumentException("Invalid filename format");
            }

            var availableFiles = Directory.GetFiles(_keysDirectory, LabFourControllerConstants.PemFormat)
                .Select(Path.GetFileName)
                .ToList();

            if (!availableFiles.Contains(filename))
            {
                throw new FileNotFoundException("File not found");
            }

            return Path.Combine(_keysDirectory, filename);
        }

        private async Task<RsaKeyDto> ExtractKeyAsync(IFormFile? keyFile, string? keyPem)
        {
            if (keyFile != null && keyFile.Length > 0)
            {
                using var keyStream = keyFile.OpenReadStream();
                using var reader = new StreamReader(keyStream);
                var pemContent = await reader.ReadToEndAsync();
                return ParsePemFile(pemContent);
            }

            if (!string.IsNullOrEmpty(keyPem))
            {
                return ParsePemFile(keyPem);
            }

            throw new ArgumentException(LabFourControllerConstants.OneOfKeysMustBeProvided);
        }

        private RsaKeyDto ParsePemFile(string pemContent)
        {
            var lines = pemContent.Split('\n');

            if (lines.Length > 0 && lines[0].StartsWith("# Created at:"))
            {
                pemContent = string.Join('\n', lines.Skip(1)).Trim();
            }

            return new RsaKeyDto { PemKey = pemContent };
        }

        private void ValidateFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException(LabFourControllerConstants.FileIsRequired);
            }
        }

        private void ValidateText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text is required");
            }
        }

        private async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> operation, string errorMessage)
        {
            try
            {
                return await operation();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (FileNotFoundException)
            {
                return NotFound(new { message = LabFourControllerConstants.KeyFileNotFound });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { message = errorMessage, error = ex.Message });
            }
        }

        private IActionResult Execute(Func<IActionResult> operation, string errorMessage)
        {
            try
            {
                return operation();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, errorMessage);
                return StatusCode(500, new { message = errorMessage, error = ex.Message });
            }
        }
    }
}