using Microsoft.AspNetCore.Mvc;
using WebApplication1.Dtos;
using WebApplication1.Interfaces.IDssService;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabFiveController : ControllerBase
{
    private readonly IDssService _service;
    private readonly ILogger<LabFiveController> _logger;

    public LabFiveController(IDssService service, ILogger<LabFiveController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost("generate-keys")]
    public async Task<IActionResult> GenerateKeys([FromBody] GenerateKeysRequestDto? request = null)
    {
        _logger.LogInformation("Запит на генерацію ключів");
        var result = await _service.GenerateKeys(
            request?.PublicKeyFileName, 
            request?.PrivateKeyFileName
        );
        return Ok(result);
    }

    [HttpGet("available-keys")]
    public IActionResult GetAvailableKeys()
    {
        _logger.LogInformation("Запит на отримання списку доступних ключів");
        var files = _service.GetAvailableKeyFiles();
        return Ok(new { files });
    }
    
    [HttpGet("download-key/{fileName}")]
    public async Task<IActionResult> DownloadKey(string fileName)
    {
        _logger.LogInformation("Запит на завантаження файлу ключа: {FileName}", fileName);
        var result = await _service.DownloadKeyFromServer(fileName);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return File(result.FileContent!, result.ContentType!, result.FileName);
    }
    
    [HttpDelete("delete-key/{fileName}")]
    public async Task<IActionResult> DeleteKey(string fileName)
    {
        _logger.LogInformation("Запит на видалення файлу ключа: {FileName}", fileName);
        var result = await _service.DeleteKeyFile(fileName);
        return Ok(result);
    }
    
    [HttpGet("public-key")]
    public IActionResult GetPublicKey()
    {
        var key = _service.GetPublicKey();
        if (string.IsNullOrEmpty(key))
        {
            return NotFound(new { message = "Публічний ключ не завантажено у пам'ять. Завантажте ключ з файлу." });
        }

        return Ok(new { publicKey = key });
    }
    
    [HttpGet("private-key")]
    public IActionResult GetPrivateKey()
    {
        var key = _service.GetPrivateKey();
        if (string.IsNullOrEmpty(key))
        {
            return NotFound(new { message = "Приватний ключ не завантажено у пам'ять. Завантажте ключ з файлу." });
        }

        return Ok(new { privateKey = key });
    }
    
    [HttpGet("keys-status")]
    public IActionResult GetKeysStatus()
    {
        return Ok(new
        {
            hasPublicKey = _service.HasPublicKey(),
            hasPrivateKey = _service.HasPrivateKey()
        });
    }

    [HttpPost("load-key")]
    public async Task<IActionResult> LoadKeyFromServer([FromBody] LoadKeyRequestDto request)
    {
        _logger.LogInformation("Запит на завантаження {KeyType} ключа з файлу: {FileName}",
            request.IsPrivateKey ? "приватного" : "публічного", request.FileName);
        var result = await _service.LoadKeyFromServer(request.FileName, request.IsPrivateKey);
        return Ok(result);
    }
    
    [HttpPost("import-key")]
    public IActionResult ImportKey([FromBody] KeyImportRequestDto request)
    {
        _logger.LogInformation("Запит на імпорт {KeyType} ключа з тексту PEM",
            request.IsPrivateKey ? "приватного" : "публічного");
        var result = _service.ImportKey(request);
        return Ok(result);
    }

    [HttpPost("import-key-file")]
    public async Task<IActionResult> ImportKeyFromFile([FromForm] FileRequestDto file, [FromForm] bool isPrivateKey)
    {
        _logger.LogInformation("Запит на імпорт {KeyType} ключа з завантаженого файлу: {FileName}",
            isPrivateKey ? "приватного" : "публічного", file?.File.FileName);
        var result = await _service.ImportKeyFromFile(file.File, isPrivateKey);
        return Ok(result);
    }
    
    [HttpPost("sign-text")]
    public IActionResult SignText([FromBody] SignatureRequestDto request)
    {
        _logger.LogInformation("Запит на підпис тексту");
        var result = _service.SignText(request);
        return Ok(result);
    }

    [HttpPost("sign-file")]
    public async Task<IActionResult> SignFile([FromForm] FileRequestDto file)
    {
        _logger.LogInformation("Запит на підпис файлу: {FileName}", file?.File.FileName);
        var result = await _service.SignFile(file.File);
        return Ok(result);
    }
    
    [HttpPost("verify-text")]
    public IActionResult VerifyText([FromBody] VerifyRequestDto request)
    {
        _logger.LogInformation("Запит на перевірку підпису тексту");
        var result = _service.VerifyText(request);
        return Ok(result);
    }

    [HttpPost("verify-file")]
    public async Task<IActionResult> VerifyFile([FromForm] FileRequestDto file, [FromForm] string signatureHex)
    {
        _logger.LogInformation("Запит на перевірку підпису файлу: {FileName}", file?.File.FileName);
        var result = await _service.VerifyFile(file.File, signatureHex);
        return Ok(result);
    }
}