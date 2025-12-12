using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VideoTools.Services;

namespace VideoTools.ViewModels
{
    public partial class DownloadToolViewModel : BaseViewModel
    {
        private readonly ProcessProviderHTTPClient httpClient = new();

        public DownloadToolViewModel() 
        {
           
        }


        [RelayCommand]
        async Task StartDownload(string uri)
        {
            await httpClient.SendDownload(uri, uri, TaskOptions.AllowCookies | TaskOptions.RemoveOnFinish);
        }
        [RelayCommand]
        async Task StartRecord(string uri)
        {
            await httpClient.SendDownload(uri, uri, TaskOptions.AllowCookies | TaskOptions.RetryOnFinish);
        }

        [RelayCommand]
        async Task StartAll()
        {
            await httpClient.StartAll();
        }
        [RelayCommand]
        async Task StopAll()
        {
            await httpClient.StopAll();
        }
    }
}
