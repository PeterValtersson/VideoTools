using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics.Arm;
using System.Security.Policy;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace VideoTools.Services
{
    [Flags]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TaskOptions
    {
        None = 0,
        RemoveOnFinish = 1,
        RetryOnFinish = 2,
        AllowCookies = 4,
    };

    public class TrackerData
    {
        public TrackerData(string displayName, string processName, string arguments/*, ListBoxItem listBoxItem*/, TaskOptions options = default)
        {
            this.displayName = displayName;
            this.arguments = arguments;
            this.processName = processName;
            statusString = "Initializing";
            //this.listBoxItem = listBoxItem;
            this.options = options;
        }
        public Process? process;
        public string arguments;
        public string statusString;
        public string processName;
        public string displayName;
        //public ListBoxItem listBoxItem;
        public TaskOptions options;
    }
    public class JSONRequestData
    {
        public string name { get; set; } = "";
        public string url { get; set; } = "";
        [JsonPropertyName("taskOptions")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TaskOptions TaskOptions { get; set;}
    }
    public class JSONResponseData
    {
        public string name { get; set; } = "";
        public string url { get; set; } = "";
        public string status { get; set; } = "";
        [JsonPropertyName("taskOptions")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TaskOptions TaskOptions { get; set; }
    }

    public class ProcessProviderService(ILogger<ProcessProviderService> _logger, IProcessServiceRequestQueue _queue, ProcessService _processService) : IContinuousWorkIteration
    {
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Stopping {GetType().Name}");
            while (!_processService.AllStopped())
            {
                _processService.StopAll();
                await _processService.UpdateTrackedTasks();
            }
        }
        public async Task Run(CancellationToken stoppingToken)
        {
            await _processService.UpdateTrackedTasks();
            await HandleRequests();
        }
        private Task HandleRequests()
        {
            while (_queue.TryDequeue(out var task))
                task.task();
            return Task.CompletedTask;
        }
              
    }

    public class ProcessService(ILogger<ProcessProviderService> _logger)
    {
        private StringBuilder StringBuilder = new StringBuilder();
        Dictionary<string, TrackerData> tracked_urls = new Dictionary<string, TrackerData>();

        public async Task UpdateTrackedTasks()
        {
            foreach (var data in tracked_urls.Values)
                await UpdateTrackedTask(data);
        }

        private async Task UpdateTrackedTask(TrackerData track_data)
        {
            if (track_data.statusString == "Initializing")
                await InitializeTask(track_data);
            else if (track_data.statusString == "Removing")
                await RemoveTrackedTask(track_data);
            else if (track_data.statusString == "Stopping")
                await StopTrackedTask(track_data);
            else
                await HandleOtherStatus(track_data);

            //Application.Current.Dispatcher.Invoke(() => { track_data.listBoxItem.Content = track_data.displayName + " " + track_data.statusString; });
        }


        private DateTime startProcessDelay = DateTime.Now;

        private Task InitializeTask(TrackerData track_data)
        {
            if (startProcessDelay.AddSeconds(5) >= DateTime.Now)
                return Task.CompletedTask;
            var exeProcess = new Process();
            exeProcess.StartInfo.CreateNoWindow = false;
            exeProcess.StartInfo.UseShellExecute = false;
            exeProcess.StartInfo.FileName = track_data.processName;
            exeProcess.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
            exeProcess.StartInfo.Arguments = (track_data.options.HasFlag(TaskOptions.AllowCookies) ? GetCookiesArguments() : "") + track_data.arguments;
            //exeProcess.StartInfo.RedirectStandardOutput = true;
            //exeProcess.OutputDataReceived += (sender, args) => StringBuilder.AppendLine(args.Data);
            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                _logger.LogInformation($"Starting new process. Command: {exeProcess.StartInfo.FileName}, Arguments: {exeProcess.StartInfo.Arguments}");
                exeProcess.Start();

                if (exeProcess is not null)
                {

                    track_data.statusString = "Starting";

                    track_data.process = exeProcess;
                    startProcessDelay = DateTime.Now;
                }
            }
            catch
            {
                _logger.LogError("Failed to start new process");
                track_data.statusString = "Removing";
            }

            return Task.CompletedTask;
        }

        private bool cookies_enabled = false;
        private string cookies_path = "";
        private string GetCookiesArguments()
        {
            if (cookies_enabled)
                return "--cookies " + cookies_path + " ";
            else
                return "";
        }

        private Task RemoveTrackedTask(TrackerData track_data)
        {
            StopTrackedTask(track_data);
            tracked_urls.Remove(track_data.arguments, out _);
            return Task.CompletedTask;
            //Application.Current.Dispatcher.Invoke(() => { var item = GetListBoxItemInTrackList(track_data.arguments); if (item != null) tracklist.Items.Remove(item); });
        }
        //private ListBoxItem? GetListBoxItemInTrackList(string site)
        //{
        //    foreach (ListBoxItem item in tracklist.Items)
        //    {
        //        if (item.Tag != null && (item.Tag as string) == site)
        //            return item;
        //    }
        //    return null;
        //}
        private Task StopTrackedTask(TrackerData track_data)
        {
            if (track_data.process != null && ProcessHelpers.StopProcess(track_data.process))
            {
                track_data.process = null;
                track_data.statusString = "Stopped";
            }
            return Task.CompletedTask;
        }


        private Task HandleOtherStatus(TrackerData track_data)
        {
            if (track_data.process is null)
                return Task.CompletedTask;

            track_data.process.Refresh();
            if (track_data.process.HasExited)
            {
                if (track_data.options.HasFlag(TaskOptions.RetryOnFinish))
                    track_data.statusString = "Initializing";
                else if (track_data.options.HasFlag(TaskOptions.RemoveOnFinish))
                    track_data.statusString = "Removing";
                else
                    track_data.statusString = "Finished";
                track_data.process = null;
            }
            else if (track_data.process.StartTime.AddSeconds(2) < DateTime.Now)
            {
                track_data.statusString = "Running";
            }

            return Task.CompletedTask;
        }

        public void EnableCookies(string file_path)
        {
            _logger.LogInformation($"Enabling cookies. File path: {cookies_path}");
            cookies_enabled = true;
            cookies_path = file_path;
        }
        public void DisableCookies()
        {
            _logger.LogInformation($"Disabling cookies.");
            cookies_enabled = false;
        }
        public void AddTask(TrackerData trackerData)
        {
            if(!IsTracked(trackerData.arguments))
                tracked_urls.Add(trackerData.arguments, trackerData);
        }
        public void StopTask(string url)
        {
            if (tracked_urls.TryGetValue(url, out var data))
            {
                if (!IsStopped(data.statusString))
                    data.statusString = "Stopping";
            }
        }
        public void UpdateOptions(string url, TaskOptions options)
        {
            if (tracked_urls.TryGetValue(url, out var data))
            {
                data.options = options;
            }
        }
        public void RemoveTask(string url)
        {
            if (tracked_urls.TryGetValue(url, out var data))
            {
                if (!IsRemoving(data.statusString))
                    data.statusString = "Removing";
            }
        }

        private bool IsRemoving(string statusString)
        {
            return statusString == "Removing";
        }

        public void StopAll()
        {
            foreach (var data in tracked_urls)
                if (!IsStopped(data.Value.statusString))
                    data.Value.statusString = "Stopping";
        }

        private static bool IsStopped(string status)
        {
            return status == "Stopped" || status == "Stopping" || status == "Finished";
        }

        public void StartAll()
        {
            foreach (var data in tracked_urls)
                if (!IsStarted(data.Value.statusString))
                    data.Value.statusString = "Initializing";
        }

        public void StartTask(string url)
        {
            if (tracked_urls.TryGetValue(url, out var data))
            {
                if (!IsStarted(data.statusString))
                    data.statusString = "Initializing";
            }
        }
        private static bool IsStarted(string status)
        {
            return status == "Initializing" || status == "Running";
        }
        public bool IsTracked(string url)
        {
            foreach (var data in tracked_urls)
                if (data.Value.arguments == url)
                    return true;
            return false;
        }
        public bool AllStopped()
        {
            foreach (var data in tracked_urls.Values)
                if (data.statusString != "Stopped")
                    return false;
            return true;
        }
        public List<JSONResponseData> GetTrackedList()
        {
            var list = new List<JSONResponseData>();
            foreach (KeyValuePair<string, TrackerData> entry in tracked_urls)
            {
                list.Add(new JSONResponseData { name = entry.Value.displayName, url = entry.Value.arguments, status = entry.Value.statusString, TaskOptions = entry.Value.options });
            }
            return list;
        }
    }
}
