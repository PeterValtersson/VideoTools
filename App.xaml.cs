using AsyncAwaitBestPractices;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel.Design.Serialization;
using System.Configuration;
using System.Data;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shell;
using VideoTools.Services;
using VideoTools.ViewModels;
using VideoTools.Views;
using Microsoft.AspNetCore.Http.Json;

namespace VideoTools
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly IHost? _host;
        private readonly WebApplication? _webApp;
        private TaskbarIcon? icon;

        public App()
        {

            var webBuilder = WebApplication.CreateBuilder();
            webBuilder.Services.AddLogging(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(LogLevel.Information);
            });
            webBuilder.Services.AddControllers();
            webBuilder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy
                        .AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            webBuilder.Services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.IncludeFields = true;
                options.SerializerOptions.PropertyNameCaseInsensitive = true;
                options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
            });

            webBuilder.Services.Configure<ContinuousBackgroundOptions>((c) => { c.cycleTime = 100; });
            webBuilder.Services.AddSingleton<ProcessService>();
            webBuilder.Services.AddContinuousBackgroundService<ProcessProviderService>();
            webBuilder.Services.AddSingleton<IProcessServiceRequestQueue>(ctx =>
            {
                if (!int.TryParse(webBuilder.Configuration["QueueCapacity"], out var queueCapacity))
                    queueCapacity = 100;
                return new ProcessServiceRequestQueue(queueCapacity, ctx.GetRequiredService<ProcessService>());
            });


            _webApp = webBuilder.Build();

            if(_webApp is null)
                throw new InvalidOperationException("WebApplication build failed.");
            _webApp.UseRouting();        // required for UseCors
            _webApp.UseCors("AllowAll"); // must come **before** MapControllers
            _webApp.MapControllers();

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddLogging(b =>
            {
                b.AddConsole();
                b.SetMinimumLevel(LogLevel.Information);
            });

            //builder.Services.Configure<ContinuousBackgroundOptions>((c)=> { c.cycleTime = 100; });
            //builder.Services.AddSingleton<ProcessService>();
            //builder.Services.AddContinuousBackgroundService<ProcessProviderService>();
            //builder.Services.AddContinuousBackgroundService<ProcessProviderHTTPServer>();
            //builder.Services.AddScoped<IHTTPServerRequestProccessingService, HTTPServerRequestProccessingService>();
            //builder.Services.AddSingleton<IProcessServiceRequestQueue>(ctx =>
            //{
            //    if (!int.TryParse(builder.Configuration["QueueCapacity"], out var queueCapacity))
            //        queueCapacity = 100;
            //    return new ProcessServiceRequestQueue(queueCapacity, ctx.GetRequiredService<ProcessService>());
            //});

            builder.Services.AddSingleton<ProcessProviderHTTPClient>();

            builder.Services.AddSingleton<MainViewModel>();
            builder.Services.AddSingleton<DownloadToolViewModel>();

            builder.Services.AddSingleton<MainWindow>();
            builder.Services.AddSingleton<DownloadToolView>();
            builder.Services.AddSingleton<VideoSplitToolView>();
            _host = builder.Build();
            if (_host is null)
                throw new InvalidOperationException("Host build failed.");
 
            var mainViewModel = _host.Services.GetRequiredService<MainViewModel>();
            mainViewModel.AddToolView("Downloader", _host.Services.GetRequiredService<DownloadToolView>());
            mainViewModel.AddToolView("Split", _host.Services.GetRequiredService<VideoSplitToolView>());

            // ServiceCollection services = new();
            // // services.AddSingleton<ProcessProviderService>();
            // services.AddHostedService<ProcessProviderHTTPServer>();
            //// services.AddHostedService(sp => sp.GetRequiredService<ProcessProviderHTTPServer>());

            // services.AddSingleton<MainViewModel>();
            // //services.AddSingleton<DownloadToolViewModel>();

            // services.AddSingleton<MainWindow>();
            // //services.AddSingleton<DownloadToolView>();
            // //services.AddSingleton<VideoSplitToolView>();

            // ////services.AddSingleton<MainWindow>(s => new MainWindow()
            // ////{
            // ////    DataContext = s.GetRequiredService<MainViewModel>()
            // ////});
            // _serviceProvider = services.BuildServiceProvider();

            //var mainViewModel = _serviceProvider.GetRequiredService<MainViewModel>();
            //mainViewModel.AddToolView("Downloader", _serviceProvider.GetRequiredService<DownloadToolView>());
            //mainViewModel.AddToolView("Split", _serviceProvider.GetRequiredService<VideoSplitToolView>());
            ////{
            ////    new TabItem { Header = "Downloader", Content = new DownloadToolView() },
            ////new TabItem { Header = "Splitter", Content = new VideoSplitToolView() }
            ////}
        }
        protected override void OnStartup(StartupEventArgs e)
        {
            _host!.StartAsync().SafeFireAndForget();
            _webApp!.StartAsync().SafeFireAndForget();

            var MainWindow = _host!.Services.GetRequiredService<MainWindow>();
            // MainWindow.Show();

            //var g = _serviceProvider.GetService<ProcessProviderHTTPServer>();
            icon = new TaskbarIcon();
            icon.IconSource = MainWindow.Icon;
            icon.Visibility = Visibility.Visible;

            var contextMenu = new ContextMenu();

            var cmi_open = new MenuItem();
            cmi_open.Header = "Open";
            cmi_open.Click += new RoutedEventHandler((object? s, RoutedEventArgs e) => { MainWindow.Show(); });
            contextMenu.Items.Add(cmi_open);

            var cmi = new MenuItem();
            cmi.Header = "Exit";
            cmi.Click += new RoutedEventHandler(ExitApp);
            contextMenu.Items.Add(cmi);

            icon.ContextMenu = contextMenu;
            icon.MenuActivation = PopupActivationMode.LeftOrRightClick;


            base.OnStartup(e);
        }
        private async void ExitApp(object? s, RoutedEventArgs e)
        {
            if(icon is not null)
                icon.Visibility = Visibility.Hidden;
            await _host!.StopAsync();
            Application.Current.Shutdown(110);
        }
    }
}
