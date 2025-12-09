using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoTools.Services.Controllers
{
    [ApiController]
    [Route("video-tools/[controller]")]
    public class DownloaderController(ILogger<Test> logger, IProcessServiceRequestQueue _processQueue) : ControllerBase
    {
        // POST
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] JSONRequestData request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Download endpoint called. Name: {}, Url: {}",
                request.name, request.url);

            var trackerData = new TrackerData(request.name, "yt-dlp.exe", request.url, request.TaskOptions);
            await _processQueue.AddTask(trackerData, cancellationToken);

            return Ok(new HTTPRequestResponseData() { result = HTTPRequestResponseData.CompletedSuccessfully });
        }

        [HttpPost("set-options")]
        public async Task<IActionResult> SetOptions([FromBody] JSONRequestData request, CancellationToken cancellationToken)
        {
            logger.LogInformation("SetOptions endpoint called. Name: {}, Url: {}",
                request.name, request.url);

            await _processQueue.SetOptions(request.url, request.TaskOptions, cancellationToken);

            return Ok(new HTTPRequestResponseData() { result = HTTPRequestResponseData.CompletedSuccessfully });
        }

        // GET
        [HttpGet("list")]
        public async Task<IActionResult> List(CancellationToken cancellationToken)
        {
            logger.LogInformation("List endpoint called.");

            var processResponse = await _processQueue.GetTrackedList(cancellationToken);
            var result = await processResponse;

            return Ok(new HTTPRequestResponseData() { result = HTTPRequestResponseData.CompletedSuccessfully, data = result });
        }

    }
}
