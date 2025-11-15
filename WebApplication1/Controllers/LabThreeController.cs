using Microsoft.AspNetCore.Mvc;
using WebApplication1.Dtos;
using WebApplication1.Interfaces.IRc5Service;
using WebApplication1.Services.RC5Service;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabThreeController : ControllerBase
    {
        private readonly IRc5Service _service;

        public LabThreeController(IRc5Service rc5Service)
        {
            _service = rc5Service;
        }

        [HttpPost("encrypt")]
        public IActionResult EncryptText([FromBody] EncryptRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var (encryptedData, iv) = _service.EncryptText(
                    request.Data,
                    request.Password,
                    request.KeySize
                );

                return Ok(new EncryptionResponseDto
                {
                    Success = true,
                    Message = "Data successfully encrypted",
                    EncryptedData = Convert.ToBase64String(encryptedData),
                    Iv = Convert.ToBase64String(iv),
                    KeySize = request.KeySize
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new EncryptionResponseDto
                {
                    Success = false,
                    Message = $"Error during encryption: {ex.Message}"
                });
            }
        }

        [HttpPost("decrypt")]
        public IActionResult DecryptText([FromBody] DecryptRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                byte[] encryptedBytes = Convert.FromBase64String(request.EncryptedData);

                string decryptedData = _service.DecryptText(
                    encryptedBytes,
                    request.Password,
                    request.KeySize
                );

                return Ok(new DecryptionResponseDto
                {
                    Success = true,
                    Message = "Data successfully encrypted",
                    DecryptedData = decryptedData
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new DecryptionResponseDto
                {
                    Success = false,
                    Message = $"Error during encryption: {ex.Message}"
                });
            }
        }
        [HttpPost("encrypt-file")]
        public async Task<IActionResult> EncryptFile([FromForm] EncryptFileRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                using var memoryStream = new MemoryStream();
                await request.File.CopyToAsync(memoryStream);
                byte[] fileBytes = memoryStream.ToArray();

                byte[] encryptedBytes = _service.EncryptBytesWithMetadata(
                    fileBytes,
                    request.Password,
                    request.File.FileName,
                    request.KeySize
                );

                string fileName = $"{Path.GetFileNameWithoutExtension(request.File.FileName)}.rc5encrypted";

                return File(encryptedBytes, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"File encryption error: {ex.Message}"
                });
            }
        }
        [HttpPost("decrypt-file")]
        public async Task<IActionResult> DecryptFile([FromForm] DecryptFileRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                using var memoryStream = new MemoryStream();
                await request.EncryptedFile.CopyToAsync(memoryStream);
                byte[] encryptedBytes = memoryStream.ToArray();

                var (decryptedBytes, metadata) = _service.DecryptBytesWithMetadata(
                    encryptedBytes,
                    request.Password,
                    request.KeySize
                );

                string fileName = $"{metadata.OriginalFileName}{metadata.OriginalExtension}";

                string contentType = GetContentType(metadata.OriginalExtension);


                return File(decryptedBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = $"File decryption error: {ex.Message}"
                });
            }
        }

        private string GetContentType(string extension)
        {
            return extension?.ToLower() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".zip" => "application/zip",
                ".rar" => "application/x-rar-compressed",
                ".mp3" => "audio/mpeg",
                ".mp4" => "video/mp4",
                ".avi" => "video/x-msvideo",
                _ => "application/octet-stream"
            };
        }
    }
}