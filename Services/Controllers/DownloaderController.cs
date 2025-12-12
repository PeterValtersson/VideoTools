using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoTools.Services.Controllers
{
    public class RequestData
    {
        public string url { get; set; }
    }
    [ApiController]
    [Route("video-tools/[controller]")]
    public class DownloaderController(ILogger<Test> logger, IProcessServiceRequestQueue _processQueue) : ControllerBase
    {
        // POST
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] JSONRequestData request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Add endpoint called. Name: {}, Url: {}",
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
        [HttpPost("stop")]
        public async Task<IActionResult> Stop([FromBody] RequestData request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Stop endpoint called. Url: {}", request.url);

            await _processQueue.StopTask(request.url, cancellationToken);

            return Ok(new HTTPRequestResponseData() { result = HTTPRequestResponseData.CompletedSuccessfully });
        }
        [HttpGet("stop-all")]
        public async Task<IActionResult> StopAll(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stop endpoint called");

            await _processQueue.StopAll(cancellationToken);

            return Ok(new HTTPRequestResponseData() { result = HTTPRequestResponseData.CompletedSuccessfully });
        }
        [HttpPost("start")]
        public async Task<IActionResult> Start([FromBody] RequestData request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Start endpoint called. Url: {}", request.url);

            await _processQueue.StartTask(request.url, cancellationToken);

            return Ok(new HTTPRequestResponseData() { result = HTTPRequestResponseData.CompletedSuccessfully });
        }
        [HttpGet("start-all")]
        public async Task<IActionResult> StartAll(CancellationToken cancellationToken)
        {
            logger.LogInformation("Start endpoint called");

            await _processQueue.StartAll(cancellationToken);

            return Ok(new HTTPRequestResponseData() { result = HTTPRequestResponseData.CompletedSuccessfully });
        }
        [HttpPost("remove")]
        public async Task<IActionResult> Remove([FromBody] RequestData request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Remove endpoint called. Url: {}", request.url);

            await _processQueue.RemoveTask(request.url, cancellationToken);

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
