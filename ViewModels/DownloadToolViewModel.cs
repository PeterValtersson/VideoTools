using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using VideoTools.Services;
using VideoTools.Settings;

namespace VideoTools.ViewModels
{
    public partial class DownloadToolViewModel : BaseViewModel
    {
        private readonly ProcessProviderHTTPClient httpClient = new();
        private readonly AppSettings _options;

        public DownloadToolViewModel(IOptions<AppSettings> options, SettingsService settingsService) 
        {
            _options = options.Value;
            cookiesUrl = _options.CookiesFilePath;
            _settinsService = settingsService;
        }
        [ObservableProperty]
        public string cookiesUrl = "";
        private readonly SettingsService _settinsService;

        partial void OnCookiesUrlChanged(string value)
        {
            _options.CookiesFilePath = value;
            _settinsService.Save(nameof(AppSettings), _options);
        }

 
        public bool EnableCookies { get => _options.EnableCookies; set { _options.EnableCookies = value; _settinsService.Save(nameof(AppSettings), _options); } }

        [ObservableProperty]
        public string url = "";

        [RelayCommand]
        async Task StartDownload(string uri)
        {
            if (uri == "")
                return;
            await httpClient.SendDownload(uri, uri, TaskOptions.RemoveOnFinish);
        }
        [RelayCommand]
        async Task StartRecord(string uri)
        {
            await httpClient.SendDownload(uri, uri, TaskOptions.RetryOnFinish);
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
