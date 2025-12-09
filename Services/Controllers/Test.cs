using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace VideoTools.Services.Controllers
{
    public class DownloadRequest
    {
        public string Url { get; set; }
        public string OutputPath { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class Test(ILogger<Test> logger) : ControllerBase
    {
        [HttpGet(Name = "list")]
        public string Get()
        {
            logger.LogInformation("Test endpoint was called.");
            return "Test successful";
        }
        [HttpPost(Name = "post")]
        public string Post()
        {
            logger.LogInformation("Test POST endpoint was called.");
            return "POST successful";
        }
        [HttpPost("data", Name = "data")]
        public string PostData([FromBody] string data)
        {
            logger.LogInformation("Test data POST endpoint was called with data: {data}", data);
            return $"Data received: {data}";
        }
        [HttpPost("download")]
        public IActionResult Download([FromBody] DownloadRequest request)
        {
            logger.LogInformation("Download endpoint called. URL: {url}, Output: {output}",
                request.Url, request.OutputPath);

            // do whatever processing you want here...

            return Ok(new { message = "Download started", request });
        }

    }
}
