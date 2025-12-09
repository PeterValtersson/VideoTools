using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace VideoTools.Services
{
    public struct ProcessRequestData
    {
        public Action task;
    }
    public interface IProcessServiceRequestQueue
    {
        public Task<Task> AddTask(TrackerData trackerData, CancellationToken cancellationToken);

        public Task<Task> StopTask(string url, CancellationToken cancellationToken);

        public Task<Task> RemoveTask(string url, CancellationToken cancellationToken);
        public Task<Task> SetOptions(string url, TaskOptions options, CancellationToken cancellationToken);

        public Task<Task> StopAll(CancellationToken cancellationToken);
        public Task<Task> StartAll(CancellationToken cancellationToken);
        public Task<Task> StartTask(string url, CancellationToken cancellationToken);
        public Task<Task> EnableCookies(string file_path, CancellationToken cancellationToken);
        public Task<Task> DisableCookies(CancellationToken cancellationToken);

        public Task<Task<bool>> IsTracked(string url, CancellationToken cancellationToken);
        public Task<Task<List<JSONResponseData>>> GetTrackedList(CancellationToken cancellationToken);
        public bool TryDequeue(out ProcessRequestData item);
    }
    public class ProcessServiceRequestQueue : IProcessServiceRequestQueue
    {
        private readonly Channel<ProcessRequestData> _queue;
        private readonly ProcessService _processService;

        public ProcessServiceRequestQueue(int capacity, ProcessService processService)
        {
            // Capacity should be set based on the expected application load and
            // number of concurrent threads accessing the queue.            
            // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
            // which completes only when space became available. This leads to backpressure,
            // in case too many publishers/calls start accumulating.
            var options = new BoundedChannelOptions(capacity)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<ProcessRequestData>(options);
            _processService = processService;
        }

        public async Task Enqueue(Action task, CancellationToken cancellationToken)
        {
            ProcessRequestData data = new ProcessRequestData();
            data.task = task;
            await _queue.Writer.WriteAsync(data, cancellationToken);
        }

        public bool TryDequeue(out ProcessRequestData item)
        {
            return _queue.Reader.TryRead(out item);
        }

        public async Task<Task> AddTask(TrackerData trackerData, CancellationToken cancellationToken)
        {
            TaskCompletionSource task = new();
            await Enqueue(() => { _processService.AddTask(trackerData); task.SetResult(); }, cancellationToken);
            return task.Task;
        }

        public async Task<Task> StopTask(string url, CancellationToken cancellationToken)
        {
            TaskCompletionSource task = new();
            await Enqueue(() => { _processService.StopTask(url); task.SetResult(); }, cancellationToken);
            return task.Task;
        }

        public async Task<Task> RemoveTask(string url, CancellationToken cancellationToken)
        {
            TaskCompletionSource task = new();
            await Enqueue(() => { _processService.RemoveTask(url); task.SetResult(); }, cancellationToken);
            return task.Task;
        }
        public async Task<Task> SetOptions(string url, TaskOptions options, CancellationToken cancellationToken)
        {
            TaskCompletionSource task = new();
            await Enqueue(() => { _processService.UpdateOptions(url, options); task.SetResult(); }, cancellationToken);
            return task.Task;
        }

        public async Task<Task> StopAll(CancellationToken cancellationToken)
        {
            TaskCompletionSource task = new();
            await Enqueue(() => { _processService.StopAll(); task.SetResult(); }, cancellationToken);
            return task.Task;
        }
        public async Task<Task> StartAll(CancellationToken cancellationToken)
        {
            TaskCompletionSource task = new();
            await Enqueue(() => { _processService.StartAll(); task.SetResult(); }, cancellationToken);
            return task.Task;
        }

        public async Task<Task> StartTask(string url, CancellationToken cancellationToken)
        {
            TaskCompletionSource task = new();
            await Enqueue(() => { _processService.StartTask(url); task.SetResult(); }, cancellationToken);
            return task.Task;
        }
        public async Task<Task> EnableCookies(string file_path, CancellationToken cancellationToken)
        {
            TaskCompletionSource task = new();
            await Enqueue(() => { _processService.EnableCookies(file_path); task.SetResult(); }, cancellationToken);
            return task.Task;
        }
        public async Task<Task> DisableCookies(CancellationToken cancellationToken)
        {
            TaskCompletionSource task = new();
            await Enqueue(() => { _processService.DisableCookies(); task.SetResult(); }, cancellationToken);
            return task.Task;
        }

        public async Task<Task<bool>> IsTracked(string url, CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool> task = new();
            await Enqueue(() => { task.SetResult(_processService.IsTracked(url)); }, cancellationToken);
            return task.Task;
        }
        public async Task<Task<List<JSONResponseData>>> GetTrackedList(CancellationToken cancellationToken)
        {
            TaskCompletionSource<List<JSONResponseData>> task = new();
            await Enqueue(() => { task.SetResult(_processService.GetTrackedList()); }, cancellationToken);
            return task.Task;
        }

    }
}
