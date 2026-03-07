using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VideoTools.Settings;

namespace VideoTools.Services.Controllers
{ 
    public class FeatureToggleDto
    {
        public bool Enabled { get; set; }
    }
    [ApiController]
    [Route("[controller]")]
    public class SettingsController(ILogger<Test> logger, IOptions<AppSettings> options, SettingsService settingsService) : ControllerBase
    {
        [HttpGet("cookies")]
        public async Task<IActionResult> GetCookiesEnabled(CancellationToken cancellationToken)
        {
            return Ok(new { enabled = options.Value.EnableCookies });
        }
        [HttpPatch("cookies")]
        public async Task<IActionResult> SetCookiesEnabled(CancellationToken cancellationToken, [FromBody] FeatureToggleDto dto)
        {
            if (dto == null)
                return BadRequest("Request body is required.");
            if(options.Value.CookiesFilePath == "")
                return Problem("Cookies file path is not configured.", statusCode: 500);
            options.Value.EnableCookies = dto.Enabled;
            settingsService.Save("AppSettings", options.Value);
            return Ok(new { enabled = options.Value.EnableCookies });

        }
    }
}
