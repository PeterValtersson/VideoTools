using AsyncAwaitBestPractices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using VideoTools.Services;
using VideoTools.Views;

namespace VideoTools.ViewModels
{
    public enum VideoToolType
    {
        Download,
        Split
    }
    public partial class VideoToolTask : ObservableObject
    {
        private readonly ProcessProviderHTTPClient? _httpClient;
        public VideoToolTask()
        {
        }
        public VideoToolTask(ProcessProviderHTTPClient httpClient)
        {
            _httpClient = httpClient;
        }
        [ObservableProperty]
        public string name = "";

        [ObservableProperty]
        public string status = "";

        public string Url { get; set; } = "";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(Visibility))]
        public VideoToolType type;

        public TaskOptions TaskOptions { get; set; }

        public bool KeepTracked
        {
            get => TaskOptions.HasFlag(TaskOptions.RetryOnFinish);
            set
            {
                if (TaskOptions.HasFlag(TaskOptions.RetryOnFinish))
                    TaskOptions = (TaskOptions | TaskOptions.RemoveOnFinish) & ~TaskOptions.RetryOnFinish;
                else
                    TaskOptions = (TaskOptions | TaskOptions.RetryOnFinish) & ~TaskOptions.RemoveOnFinish;

                OnPropertyChanged();

                KeepTrackedChanged().SafeFireAndForget();
            }
        }
        public Visibility Visibility { get { if (Type == VideoToolType.Download) return Visibility.Visible; else return Visibility.Hidden; } }

        private async Task KeepTrackedChanged()
        {
            if (_httpClient is null)
                return;
            await _httpClient.UpdateOptions(Url, TaskOptions);
        }
    }
    //public class TabContentData
    //{
    //    public string Header { get; set; }
    //    public TabItem
    //}
    public partial class MainViewModel : BaseViewModel
    {
        public BindingList<VideoToolTask> videoToolTasks { get; set; } = new();
        public ObservableCollection<TabItem> tabItems { get; set; } = new();
        //{
        //    new TabItem{Header = "Downloader", Content = new DownloadToolView()},
        //    new TabItem{Header = "Splitter", Content = new VideoSplitToolView()}
        //};
        private readonly System.Timers.Timer _timer = new(100);
        private readonly ProcessProviderHTTPClient _httpClient = new();
        private CancellationTokenSource? _cancellationTokenSource;
        public MainViewModel()
        {
            Title = "Video Toolkit";
            _timer.Elapsed += new ElapsedEventHandler(async (o, a) => { await GetTasks(); });
        }

        ~MainViewModel()
        {
            _timer.Dispose();
        }

        [RelayCommand]
        public void StopUpdate()
        {
            _timer.Stop();
            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
        [RelayCommand]
        public void StartUpdate()
        {
            if (_cancellationTokenSource is null)
                _cancellationTokenSource = new();
            _timer.Start();
        }

        public void AddToolView(string header, UserControl control)
        {
            tabItems.Add(new TabItem { Header = header, Content = control });
        }

        private CancellationToken GetCancellationToken()
        {
            if (_cancellationTokenSource is null)
                return default;
            return _cancellationTokenSource.Token;
        }

        private async Task GetTasks()
        {
            if (IsBusy) return;
            IsBusy = true;
            //if (IsBusy)
            //    return;
            //IsBusy = true;
            ////App.Current.Dispatcher.Invoke(() =>
            ////{
            ////    videoToolTasks.Add(new() { Name = "Test1", Status = "Running" });
            ////    videoToolTasks.Add(new() { Name = "Test2", Status = "Running" });
            ////});
            /// 

            try
            {
                var taskList = await _httpClient.GetTaskList(GetCancellationToken()).ConfigureAwait(false);
                var tasksToAdd = new List<VideoToolTask>();
                foreach (var task in taskList)
                {
                    try
                    {
                        var item = videoToolTasks.Single(findTask => findTask.Name == task.name);
                        item.Status = task.status;
                        item.TaskOptions = task.TaskOptions;
                    }
                    catch (InvalidOperationException) // Not found add it to the list
                    {
                        tasksToAdd.Add(new(_httpClient) { Name = task.name, Status = task.status, Url = task.url, TaskOptions = task.TaskOptions });
                    }
                }
                var tasksToRemove = new List<VideoToolTask>();
                foreach (var task in videoToolTasks)
                {
                    try
                    {
                        var item = taskList.Single(findTask => findTask.name == task.Name);
                        // Do nothing
                    }
                    catch (InvalidOperationException) // Not found, remove from list
                    {
                        tasksToRemove.Add(task);
                    }
                }

                if (tasksToAdd.Count > 0 || tasksToRemove.Count > 0)
                {
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var task in tasksToAdd)
                            videoToolTasks.Add(task);
                        foreach (var task in tasksToRemove)
                            videoToolTasks.Remove(task);
                    });
                }


            }
            catch (OperationCanceledException)
            {
                // Ignore
            }
            catch (HttpRequestException)
            {
                // Ignore, shutdown happened on server side first
            }

            IsBusy = false;
            //IsBusy = false;
        }

    }
}
