using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BlacklistApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlacklistController : ControllerBase
    {
        private readonly ILogger<BlacklistController> _logger;
        private readonly string _filePath = "/app/Logs/blacklist/blacklist.txt";

        public BlacklistController(ILogger<BlacklistController> logger)
        {
            _logger = logger;

            // Crear el archivo si no existe
            if (!System.IO.File.Exists(_filePath))
            {
                System.IO.File.Create(_filePath).Close();
            }
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddUrl([FromBody] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest("URL cannot be empty");
            }

            var urls = await System.IO.File.ReadAllLinesAsync(_filePath);

            if (urls.Contains(url))
            {
                return Conflict("URL already exists in the blacklist");
            }

            await System.IO.File.AppendAllTextAsync(_filePath, url + "\n");
            _logger.LogInformation($"Added URL to blacklist: {url}");

            return Ok("URL added to blacklist");
        }

        [HttpDelete("remove")]
        public async Task<IActionResult> RemoveUrl([FromBody] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest("URL cannot be empty");
            }

            var urls = await System.IO.File.ReadAllLinesAsync(_filePath);

            if (!urls.Contains(url))
            {
                return NotFound("URL not found in the blacklist");
            }

            var updatedUrls = urls.Where(u => u != url).ToArray();
            await System.IO.File.WriteAllLinesAsync(_filePath, updatedUrls);
            _logger.LogInformation($"Removed URL from blacklist: {url}");

            return Ok("URL removed from blacklist");
        }

        [HttpGet("check")]
        public async Task<IActionResult> CheckUrl([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest("URL cannot be empty");
            }

            var urls = await System.IO.File.ReadAllLinesAsync(_filePath);

            if (urls.Contains(url))
            {
                _logger.LogInformation($"URL found in blacklist: {url}");
                return Ok(true);
            }

            _logger.LogInformation($"URL not found in blacklist: {url}");
            return Ok(false);
        }
    }
}
