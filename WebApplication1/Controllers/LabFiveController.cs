using Microsoft.AspNetCore.Mvc;
using WebApplication1.Dtos;
using WebApplication1.Interfaces.IDssService;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LabFiveController : ControllerBase
{
    private readonly IDssService _service;

    public LabFiveController(IDssService service)
    {
        _service = service;
    }

    [HttpPost("generate-keys")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GenerateKeys([FromBody] GenerateKeysRequestDto? request = null)
    {
        var result = await _service.GenerateKeys(
            request?.PublicKeyFileName, 
            request?.PrivateKeyFileName
        );
        return Ok(result);
    }

    [HttpGet("available-keys")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetAvailableKeys()
    {
        var files = _service.GetAvailableKeyFiles();
        return Ok(new { files });
    }
    
    [HttpGet("download-key/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadKey(string fileName)
    {
        var result = await _service.DownloadKeyFromServer(fileName);
        
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return File(result.FileContent!, result.ContentType!, result.FileName);
    }
    
    [HttpDelete("delete-key/{fileName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteKey(string fileName)
    {
        var result = await _service.DeleteKeyFile(fileName);
        return Ok(result);
    }
    
    [HttpGet("public-key")]
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetKeysStatus()
    {
        return Ok(new
        {
            hasPublicKey = _service.HasPublicKey(),
            hasPrivateKey = _service.HasPrivateKey()
        });
    }

    [HttpPost("load-key")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> LoadKeyFromServer([FromBody] LoadKeyRequestDto request)
    {
        var result = await _service.LoadKeyFromServer(request.FileName, request.IsPrivateKey);
        return Ok(result);
    }
    
    [HttpPost("import-key")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult ImportKey([FromBody] KeyImportRequestDto request)
    {
        var result = _service.ImportKey(request);
        return Ok(result);
    }

    [HttpPost("import-key-file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ImportKeyFromFile([FromForm] FileRequestDto file, [FromForm] bool isPrivateKey)
    {
        var result = await _service.ImportKeyFromFile(file.File, isPrivateKey);
        return Ok(result);
    }
    
    [HttpPost("sign-text")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult SignText([FromBody] SignatureRequestDto request)
    {
        var result = _service.SignText(request);
        return Ok(result);
    }

    [HttpPost("sign-file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> SignFile([FromForm] FileRequestDto file)
    {
        var result = await _service.SignFile(file.File);
        return Ok(result);
    }
    
    [HttpPost("verify-text")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult VerifyText([FromBody] VerifyRequestDto request)
    {
        var result = _service.VerifyText(request);
        return Ok(result);
    }

    [HttpPost("verify-file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyFile([FromForm] FileRequestDto file, [FromForm] string signatureHex)
    {
        var result = await _service.VerifyFile(file.File, signatureHex);
        return Ok(result);
    }
}