using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace kanbanBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LabelConfigController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<LabelConfigController> _logger;
        private readonly string _configPath;

        public LabelConfigController(IWebHostEnvironment env, ILogger<LabelConfigController> logger)
        {
            _env = env;
            _logger = logger;
            // Points at wwwroot/assets/label-config.json
            _configPath = Path.Combine(_env.WebRootPath, "assets", "label-config.json");
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            if (!System.IO.File.Exists(_configPath))
                return NotFound("label-config.json not found");

            var json = await System.IO.File.ReadAllTextAsync(_configPath, Encoding.UTF8);
            return Content(json, "application/json", Encoding.UTF8);
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] object config)
        {
            try
            {
                var formatted = System.Text.Json.JsonSerializer.Serialize(
                    config,
                    new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
                );
                await System.IO.File.WriteAllTextAsync(_configPath, formatted, Encoding.UTF8);
                return Ok(new { status = "ok" });
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Failed to write label-config.json");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
