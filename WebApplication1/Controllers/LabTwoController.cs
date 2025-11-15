using Microsoft.AspNetCore.Mvc;
using WebApplication1.Interfaces.IMD5Service;
using WebApplication1.Services.MD5Service;

namespace WebApplication1.Controllers
{
    [Controller]
    [Route("api/[controller]")]
    public class LabTwoController : ControllerBase
    {
        private readonly IMd5Service _md5Service;
        public LabTwoController(IMd5Service md5Service)
        {
            _md5Service = md5Service;
        }

        [HttpGet("GetHash")]
        public async Task<IActionResult> GetHash([FromQuery] string? input)
        {
            input = HttpContext.Request.Query["input"].ToString();

            if (input == null)
            {
                return BadRequest("Input cannot be null");
            }
            var hash = await _md5Service.ComputeMD5Hash(input);
            return Ok(hash);
        }

        [HttpPost("HashFile")]
        public async Task<IActionResult> HashFileOrArchive(IFormFile file, [FromServices] Md5Service md5Service)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is empty");

            string extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            md5Service.Initialize();

            if (extension == ".zip")
            {
                using var archiveStream = file.OpenReadStream();
                using var archive = new System.IO.Compression.ZipArchive(archiveStream, System.IO.Compression.ZipArchiveMode.Read);

                byte[] buffer = new byte[81920];

                foreach (var entry in archive.Entries.OrderBy(e => e.FullName))
                {
                    if (entry.Length == 0) continue;

                    using var entryStream = entry.Open();
                    int bytesRead;
                    while ((bytesRead = await entryStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        md5Service.TransformBlock(buffer, 0, bytesRead);
                    }
                }

                string hash = md5Service.TransformFinalBlock();
                return Ok(hash);
            }
            else
            {
                using var stream = file.OpenReadStream();
                byte[] buffer = new byte[81920];
                int bytesRead;

                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    md5Service.TransformBlock(buffer, 0, bytesRead);
                }

                string hash = md5Service.TransformFinalBlock();
                return Ok(hash);
            }
        }
    }
}
