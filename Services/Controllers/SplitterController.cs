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
    public class SplitterController(ILogger<Test> logger, IProcessServiceRequestQueue _processQueue, IOptions<AppSettings> options) : ControllerBase
    {
        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] JSONRequestData request, CancellationToken cancellationToken)
        {
            logger.LogInformation("Splitter Add endpoint called. Name: {}, Url: {}",
                request.name, request.url);

            var arguments = options.Value.EnableCookies && options.Value.CookiesFilePath != "" ?
                                $"--cookies {options.Value.CookiesFilePath} {request.url}" :
                                request.url;


            var trackerData = new TrackerData(request.name, "yt-dlp.exe", arguments, request.TaskOptions);
            await _processQueue.AddTask(trackerData, cancellationToken);

            return Ok(new HTTPRequestResponseData() { result = HTTPRequestResponseData.CompletedSuccessfully });
        }

    }
}
