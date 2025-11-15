using Microsoft.AspNetCore.Mvc;
using WebApplication1.Interfaces.IMD5Service;
using WebApplication1.Services.MD5Service;
using System.IO.Compression;

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabTwoController : ControllerBase
    {
        private readonly IMd5Service _md5Service;
        private readonly ILogger<LabTwoController> _logger;
        private const long MaxFileSize = unchecked(5 * 1024 * 1024 * 1024);

        public LabTwoController(IMd5Service md5Service, ILogger<LabTwoController> logger)
        {
            _md5Service = md5Service;
            _logger = logger;
        }

        [HttpGet("GetHash")]
        public async Task<IActionResult> GetHash([FromQuery] string input)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(input))
            {
                return BadRequest(new { message = "Input cannot be null or empty" });
            }

            try
            {
                var hash = await _md5Service.ComputeMD5Hash(input);
                return Ok(new { hash = hash });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing hash for input");
                return StatusCode(500, new { message = "Error computing hash" });
            }
        }

        [HttpPost("HashFile")]
        public async Task<IActionResult> HashFileOrArchive(IFormFile file, [FromServices] Md5Service md5Service)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "File is empty or not provided" });
            }

            if (file.Length > MaxFileSize)
            {
                return BadRequest(new { message = $"File size exceeds maximum allowed size"});
            }

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (string.IsNullOrEmpty(extension) || (!extension.Equals(".zip") && !extension.Equals(".bin")))
            {
                return BadRequest(new { message = "Only .zip and binary files are supported" });
            }

            try
            {
                md5Service.Initialize();

                string hash;

                if (extension == ".zip")
                {
                    hash = await HashZipArchive(file, md5Service);
                }
                else
                {
                    hash = await HashRegularFile(file, md5Service);
                }

                return Ok(new { hash = hash, fileName = file.FileName });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Invalid file format: {FileName}", file.FileName);
                return BadRequest(new { message = "Invalid file format" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hashing file: {FileName}", file.FileName);
                return StatusCode(500, new { message = "Error processing file" });
            }
        }

        private async Task<string> HashZipArchive(IFormFile file, Md5Service md5Service)
        {
            using var archiveStream = file.OpenReadStream();
            using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read);

            byte[] buffer = new byte[81920];

            foreach (var entry in archive.Entries.OrderBy(e => e.FullName))
            {
                if (entry.Length == 0)
                {
                    continue;
                }

                if (entry.Length > MaxFileSize)
                {
                    throw new InvalidOperationException($"Archive entry exceeds maximum allowed size: {entry.FullName}");
                }

                using var entryStream = entry.Open();
                int bytesRead;

                while ((bytesRead = await entryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    md5Service.TransformBlock(buffer, 0, bytesRead);
                }
            }

            return md5Service.TransformFinalBlock();
        }

        private async Task<string> HashRegularFile(IFormFile file, Md5Service md5Service)
        {
            using var stream = file.OpenReadStream();
            byte[] buffer = new byte[81920];
            int bytesRead;

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                md5Service.TransformBlock(buffer, 0, bytesRead);
            }

            return md5Service.TransformFinalBlock();
        }
    }
}