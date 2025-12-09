//using AsyncAwaitBestPractices;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using System.Diagnostics;
//using System.IO;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace VideoTools.Services
//{
//    public class ProcessProviderHTTPServer(ILogger<ProcessProviderHTTPServer> _logger, IServiceProvider _services) : IContinuousWorkIteration
//    {
//        private static string url = "http://localhost:6389/";

//        private HttpListener _listener = new();
//        public Task Start(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("Starting");
//            _listener.Prefixes.Add(url);
//            _listener.Start();
//            return Task.CompletedTask;
//        }
//        public Task StopAsync(CancellationToken cancellationToken)
//        {
//            _logger.LogInformation("Stopping");
//            _listener.Stop();
//            return Task.CompletedTask;
//        }
//        public async Task Run(CancellationToken stoppingToken)
//        {
//            try
//            {
//                var context = await _listener.GetContextAsync().ConfigureAwait(false);
//                using (var scope = _services.CreateScope())
//                {
//                    var scopedProcessingService = scope.ServiceProvider.GetRequiredService<IHTTPServerRequestProccessingService>();
//                    scopedProcessingService.ProcessRequest(stoppingToken, context).SafeFireAndForget((e)=> 
//                    {
//                        _logger.LogError($"Exception when processing HTTP request: {e.Message}");
//                    });
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogInformation($"{ex.Message}");

//                if (stoppingToken.IsCancellationRequested)
//                    _logger.LogInformation("Listener connection closed due to cancellation");
//                else
//                    _logger.LogError($"HTTP server exception: {ex.Message}");
//            }
//        }
//    }


//    internal interface IHTTPServerRequestProccessingService
//    {
//        Task ProcessRequest(CancellationToken stoppingToken, HttpListenerContext context);
//    }

//    internal class HTTPServerRequestProccessingService(ILogger<HTTPServerRequestProccessingService> _logger, IProcessServiceRequestQueue _processQueue) : IHTTPServerRequestProccessingService
//    {
//        private static Dictionary<string, Func<JSONRequestData?, CancellationToken, Task<HTTPRequestResponseData>>>? _postHandlers;
//        private static Dictionary<string, Func<JSONRequestData?, CancellationToken, Task<HTTPRequestResponseData>>>? _getHandlers;
//        private static readonly Dictionary<string, Dictionary<string, Func<JSONRequestData?, CancellationToken, Task<HTTPRequestResponseData>>>> _endpointHandlers;
//        static HTTPServerRequestProccessingService()
//        {
//            _postHandlers = new(StringComparer.OrdinalIgnoreCase)
//            {
//                ["/download"] = async (data, token) => await ProcessDownload(data, token),
//                ["/updateOptions"] = async (data, token) => await ProcessUpdateOptions(data, token),
//            };
//            _getHandlers = new(StringComparer.OrdinalIgnoreCase)
//            {
//                ["/list"] = async (data, token) => await ProcessList(data, token),
//            };
//            _endpointHandlers = new(StringComparer.OrdinalIgnoreCase)
//            {
//                ["post"] = _postHandlers,
//                ["get"] = _getHandlers,
//            };
//        }
//        public async Task ProcessRequest(CancellationToken stoppingToken, HttpListenerContext context)
//        {

//            var req = context.Request;

//            if (req.Url is null)
//            {
//                _logger.LogError("Request with no URL given");
//                return;
//            }

//            //_logger.LogInformation(req.HttpMethod);
//            //_logger.LogInformation(req.UserHostName);
//            //_logger.LogInformation(req.UserAgent);

//            if(req.Url.AbsolutePath != "/list")
//                _logger.LogInformation("Processing HTTP request. Command: {Command}", req.Url.AbsolutePath);

//            try
//            {
//                var method = _endpointHandlers![req.HttpMethod];
//            }
//            catch(KeyNotFoundException ex)
//            {

//            }
//            var handler = _endpointHandlers![req.HttpMethod][req.Url!.AbsolutePath];
            
//            var resp = context.Response;
//            HTTPRequestResponseData responseData;
//            if (_endpointHandlers!.TryGetValue(req.Url!.AbsolutePath, out var handler))
//            {
//                var reqData = DeserializeRequest(req.InputStream);
//                responseData = await handler.Invoke(reqData, stoppingToken);
//            }
//            else
//            {
//                responseData = new HTTPRequestResponseData() { result = "Unsupported endpoint" };
//            }
                


//            var json = JsonSerializer.Serialize(responseData);
//            byte[] data = Encoding.UTF8.GetBytes(json);

//            resp.ContentType = "application/json";
//            resp.ContentEncoding = Encoding.UTF8;
//            resp.ContentLength64 = data.LongLength;

//            // Write out to the response stream (asynchronously), then close it
//            await resp.OutputStream.WriteAsync(data, 0, data.Length);
//            resp.Close();
//        }
   
//        private async Task<HTTPRequestResponseData> HandleRequest(HttpListenerRequest request, CancellationToken stoppingToken)
//        {
//            if (request.HttpMethod == "POST")
//            {
//                var reqData = DeserializeRequest(request.InputStream);
//                if (request.Url!.AbsolutePath == "/download")
//                    return await ProcessDownload(reqData, stoppingToken);
//                else if(request.Url!.AbsolutePath == "/updateOptions")
//                    return await ProcessUpdateOptions(reqData, stoppingToken);
//                else
//                {
//                    return new HTTPRequestResponseData() { result = "Unsupported endpoint" };
//                }
//            }
//            else if (request.HttpMethod == "GET")
//            {
//                if (request.Url!.AbsolutePath == "/list")
//                    return await ProcessList(stoppingToken);
//            }

//            return new HTTPRequestResponseData() { result = "Unsupported method" };
//        }
//        static private JSONRequestData DeserializeRequest(Stream stream)
//        {
//            var options = new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true }; ;
//            options.Converters.Add(new JsonStringEnumConverter());
//            string json = new StreamReader(stream).ReadToEnd();
//            JSONRequestData res = JsonSerializer.Deserialize<JSONRequestData>(json, options) ?? throw new Exception("JSON Deserialize failed");
//            return res;
//        }
//        static private async Task<HTTPRequestResponseData> ProcessDownload(JSONRequestData? data, CancellationToken stoppingToken)
//        {
//            var trackerData = new TrackerData(data.name, "yt-dlp.exe", data.url, TaskOptions.RemoveOnFinish | TaskOptions.AllowCookies);
//            await _processQueue.AddTask(trackerData, stoppingToken);

//            return new HTTPRequestResponseData() { result = "Completed Successfully" };
//        }
//        static private async Task<HTTPRequestResponseData> ProcessUpdateOptions(JSONRequestData? data, CancellationToken stoppingToken)
//        {
//            await _processQueue.SetOptions(data.url, data.TaskOptions, stoppingToken);

//            return new HTTPRequestResponseData() { result = "Completed Successfully" };
//        }
//        static private async Task<HTTPRequestResponseData> ProcessList(JSONRequestData? data, CancellationToken stoppingToken)
//        {
//            var processResponse = await _processQueue.GetTrackedList(stoppingToken);
//            var result = await processResponse;

//            return new HTTPRequestResponseData() { result = "Completed Successfully", data = result };
//        }

//        //private JSONRequestData DeserializeRequest(Stream stream)
//        //{
//        //    string json = new StreamReader(stream).ReadToEnd();

//        //    return JsonSerializer.Deserialize<JSONRequestData>(json) ?? throw new Exception("JSON Deserialize failed");
//        //}
//        //private async Task HandleHTTPRequests(CancellationToken stoppingToken)
//        //{
//        //    try
//        //    {
//        //        var context = await httpListener.GetContextAsync();
//        //        await _processTaskQueue.QueueBackgroundWorkItemAsync(async Task(CancellationToken stoppingToken) => { await ProccessRequst(context); });

//        //    }
//        //    catch (Exception e) { }
//        //}
//        //private async Task ProccessRequst(HttpListenerContext context)
//        //{
//        //    var req = context.Request;
//        //    var resp = context.Response;

//        //    _logger.LogInformation("Request #: {0}", ++requestCount);
//        //    if (req.Url is null)
//        //        return;

//        //    _logger.LogInformation(req.HttpMethod);
//        //    _logger.LogInformation(req.UserHostName);
//        //    _logger.LogInformation(req.UserAgent);

//        //    if (req.HttpMethod == "POST")
//        //    {
//        //        if (req.Url.AbsolutePath == "/download")
//        //        {
//        //            var reqData = DeserializeRequest(req.InputStream);
//        //            if (!await _processProvider.IsTracked(reqData.url))
//        //            {
//        //                var trackerData = new TrackerData(reqData.name, "yt-dlp.exe", reqData.url, TaskOptions.RemoveOnFinish | TaskOptions.AllowCookies);
//        //                _processProvider.AddTask(trackerData);
//        //            }
//        //        }
//        //        else if (req.Url.AbsolutePath == "/record")
//        //        {
//        //            var reqData = DeserializeRequest(req.InputStream);
//        //            if (!await _processProvider.IsTracked(reqData.url))
//        //            {
//        //                var trackerData = new TrackerData(reqData.name, "yt-dlp.exe", reqData.url, TaskOptions.RetryOnFinish | TaskOptions.AllowCookies);
//        //                _processProvider.AddTask(trackerData);
//        //            }
//        //        }
//        //        else if (req.Url.AbsolutePath == "/start")
//        //        {
//        //            var reqData = DeserializeRequest(req.InputStream);
//        //            if (await _processProvider.IsTracked(reqData.url))
//        //                _processProvider.StartTask(reqData.url);
//        //        }
//        //        else if (req.Url.AbsolutePath == "/remove")
//        //        {
//        //            var reqData = DeserializeRequest(req.InputStream);
//        //            if (await _processProvider.IsTracked(reqData.url))
//        //                _processProvider.RemoveTask(reqData.url);
//        //        }
//        //        else if (req.Url.AbsolutePath == "/pause")
//        //        {
//        //            var reqData = DeserializeRequest(req.InputStream);
//        //            if (await _processProvider.IsTracked(reqData.url))
//        //                _processProvider.StopTask(reqData.url);
//        //        }

//        //        byte[] data = Encoding.UTF8.GetBytes("Hello");
//        //        resp.ContentType = "text/html";
//        //        resp.ContentEncoding = Encoding.UTF8;
//        //        resp.ContentLength64 = data.LongLength;

//        //        // Write out to the response stream (asynchronously), then close it
//        //        await resp.OutputStream.WriteAsync(data, 0, data.Length);
//        //        resp.Close();
//        //    }
//        //    else if (req.HttpMethod == "GET")
//        //    {
//        //        if (req.Url.AbsolutePath == "/list")
//        //        {
//        //            var trackedList = await _processProvider.GetTrackedList();

//        //            var json = JsonSerializer.Serialize(trackedList);
//        //            byte[] data = Encoding.UTF8.GetBytes(json);
//        //            resp.ContentType = "application/json";
//        //            resp.ContentEncoding = Encoding.UTF8;
//        //            resp.ContentLength64 = data.LongLength;

//        //            // Write out to the response stream (asynchronously), then close it
//        //            await resp.OutputStream.WriteAsync(data, 0, data.Length);
//        //            resp.Close();
//        //        }
//        //    }

//        //}
//    }
//}
