using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace VideoTools.Services
{
    public class ProcessProviderHTTPClient
    {
        private string uri = "http://localhost:5000";
        private HttpClient httpClient = new();
        public ProcessProviderHTTPClient()
        {
            httpClient.BaseAddress = new Uri(uri);
        }

        public async Task SendDownload(string displayName, string arguments, TaskOptions options = TaskOptions.AllowCookies | TaskOptions.RemoveOnFinish)
        {
            await httpClient.PostAsJsonAsync("video-tools/downloader/add", new JSONRequestData { name = displayName, url = arguments, TaskOptions = options });
        }
        public async Task UpdateOptions(string url, TaskOptions options)
        {
            var soptions = new JsonSerializerOptions() { WriteIndented = true, IncludeFields = true }; 
            soptions.Converters.Add(new JsonStringEnumConverter());
            await httpClient.PostAsJsonAsync("video-tools/downloader/set-options", new JSONRequestData { url = url, TaskOptions = options }, soptions);
        }
        private JsonDocument DeserializeRequest(Stream stream)
        {
            string json = new StreamReader(stream).ReadToEnd();
            return JsonDocument.Parse(json);
        }
        private void EnsureCorrectResponse(JsonDocument doc)
        {
            var result = doc.RootElement.GetProperty("result").Deserialize<string>();
            if (result != HTTPRequestResponseData.CompletedSuccessfully)
                throw new Exception($"HTTP Request failed: {result}");
        }
        private List<JSONResponseData> DeserializeList(JsonDocument doc)
        {
            var soptions = new JsonSerializerOptions() { IncludeFields = true }; 
            soptions.Converters.Add(new JsonStringEnumConverter());
            return doc.RootElement.GetProperty("data").Deserialize<List<JSONResponseData>>(soptions) ?? throw new Exception("Deserialize failed of list");
        }
        public async Task<List<JSONResponseData>> GetTaskList(CancellationToken cancellationToken)
        {
            var response = await httpClient.GetAsync("video-tools/downloader/list", cancellationToken);
            response.EnsureSuccessStatusCode();
            var json = DeserializeRequest(await response.Content.ReadAsStreamAsync(cancellationToken));
            EnsureCorrectResponse(json);
            return DeserializeList(json);
        }
    }
}
