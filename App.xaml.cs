// App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using System;
using System.Net;
using System.Net.Http;
using System.Windows;


namespace SigstreamTelemetryAgent
{
    public partial class App : Application
    {
        public static IHost HostInstance { get; private set; } = default!;
        private static System.Threading.Mutex? _singleInstance;

        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(path: System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "SigStream", "logs", "agent-.log"),
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Single-instance guard
            bool createdNew;
            _singleInstance = new System.Threading.Mutex(true, "SigstreamTelemetryAgent_Mutex", out createdNew);
            if (!createdNew)
            {
                MessageBox.Show("SigStream Agent is already running.", "SigStream",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            HostInstance = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((ctx, services) =>
                {
                    // Core services
                    services.AddSingleton<Services.ISettingsService, Services.SettingsService>();
                    services.AddSingleton<Services.IHeartbeatService, Services.HeartbeatService>();
                    services.AddSingleton<Services.IComPortService, Services.ComPortService>();
                    services.AddSingleton<Services.IOfflineQueue, Services.OfflineQueue>();
                    services.AddSingleton<Services.IToastService, Services.ToastService>();
                    services.AddSingleton<Services.ITrayService, Services.TrayService>();
                    services.AddSingleton<Services.IStartupService, Services.StartupService>();

                    // HttpClient + Polly for IApiClient  (replaces the old AddSingleton<IApiClient, ApiClient>())
                    services
                        .AddHttpClient<Services.IApiClient, Services.ApiClient>()
                        .ConfigureHttpClient((sp, client) =>
                        {
                            var s = sp.GetRequiredService<Services.ISettingsService>().Load();
                            client.BaseAddress = new Uri(s.ApiBaseUrl ?? "https://sigstreamcloud.com");
                            client.Timeout = TimeSpan.FromSeconds(15);
                        })
                        .AddPolicyHandler(GetRetryPolicy())
                        .AddPolicyHandler(GetCircuitBreakerPolicy());

                    // Background 60s offline-queue flush
                    services.AddHostedService<Services.QueueFlushService>();

                    // ViewModels
                    services.AddSingleton<ViewModels.MainWindowViewModel>();
                    services.AddSingleton<ViewModels.DashboardViewModel>();
                    services.AddSingleton<ViewModels.ApiConfigViewModel>();
                    services.AddSingleton<ViewModels.ComPortViewModel>();
                    services.AddSingleton<ViewModels.AboutViewModel>();

                    // Views
                    services.AddSingleton<Views.MainWindow>();
                    services.AddSingleton<Views.Pages.DashboardPage>();
                    services.AddSingleton<Views.Pages.ApiConfigPage>();
                    services.AddSingleton<Views.Pages.ComPortPage>();
                    services.AddSingleton<Views.Pages.AboutPage>();
                })
                .Build();

            HostInstance.Start();

            // Apply “Start with Windows” setting to Run key
            var settingsSvc = HostInstance.Services.GetRequiredService<Services.ISettingsService>();
            var settings = settingsSvc.Load();
            var startup = HostInstance.Services.GetRequiredService<Services.IStartupService>();
            startup.SetEnabled(settings.AutoStartOnBoot);

            // Show main window (optionally minimized to tray)
            var main = HostInstance.Services.GetRequiredService<Views.MainWindow>();
            if (settings.StartMinimized)
            {
                main.WindowState = WindowState.Minimized;
                main.ShowInTaskbar = false;
                main.Show();
                main.Hide();
            }
            else
            {
                main.Show();
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await HostInstance.StopAsync();
            HostInstance.Dispose();
            Log.CloseAndFlush();

            _singleInstance?.ReleaseMutex();
            _singleInstance?.Dispose();

            base.OnExit(e);
        }

        // Tray context menu handlers
        private void TrayOpen_Click(object sender, RoutedEventArgs e)
        {
            var win = HostInstance.Services.GetRequiredService<Views.MainWindow>();
            win.ShowInTaskbar = true;
            if (!win.IsVisible) win.Show();
            if (win.WindowState == WindowState.Minimized) win.WindowState = WindowState.Normal;
            win.Activate();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e) =>
            Current.Shutdown();

        private void TrayPause_Checked(object sender, RoutedEventArgs e)
        {
            var hb = HostInstance.Services.GetRequiredService<Services.IHeartbeatService>();
            hb.Stop();
        }

        private void TrayPause_Unchecked(object sender, RoutedEventArgs e)
        {
            var s = HostInstance.Services.GetRequiredService<Services.ISettingsService>().Load();
            var hb = HostInstance.Services.GetRequiredService<Services.IHeartbeatService>();
            hb.Start(s);
        }

        // --- Polly policies ---
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
            HttpPolicyExtensions.HandleTransientHttpError() // 5xx, 408, HttpRequestException
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests) // 429
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Min(0.5 * Math.Pow(2, attempt - 1), 15)) // 0.5,1,2,4,8s (cap 15)
                        + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)));

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
            HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 8,
                    durationOfBreak: TimeSpan.FromSeconds(60));
    }
}
