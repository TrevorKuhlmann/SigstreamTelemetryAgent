// App.xaml.cs — CLEAN PRODUCTION + Branded Dialogs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Extensions.Http;
using SigstreamTelemetryAgent.Services;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace SigstreamTelemetryAgent
{
    public partial class App : Application
    {
        public static IHost HostInstance { get; private set; } = default!;
        private static System.Threading.Mutex? _singleInstance;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Single-instance guard
            bool createdNew;
            _singleInstance = new System.Threading.Mutex(true, "SigstreamTelemetryAgent_Mutex", out createdNew);
            if (!createdNew)
            {
                ShowInfo("SigStream Agent is already running.", "Already running");
                Shutdown();
                return;
            }

            // Minimal exception hooks (use branded dialog if available; fallback to MessageBox)
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                ShowError("A fatal error occurred and the application will close.", "Fatal error");
                Shutdown();
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                ShowError("An unexpected error occurred and the application will close.", "Unexpected error");
                ex.Handled = true;
                Shutdown();
            };

            TaskScheduler.UnobservedTaskException += (s, ex) =>
            {
                // Mark observed to avoid process crash in finalizer thread
                ex.SetObserved();
            };

            try
            {
                HostInstance = Host.CreateDefaultBuilder()
                    .ConfigureServices((ctx, services) =>
                    {
                        // Services
                        services.AddSingleton<ISettingsService, SettingsService>();
                        services.AddSingleton<IHeartbeatService, HeartbeatService>();
                        services.AddSingleton<IComPortService, ComPortService>();
                        services.AddSingleton<IOfflineQueue, OfflineQueue>();
                        services.AddSingleton<IToastService, ToastService>();
                        services.AddSingleton<ITrayService, TrayService>();
                        services.AddSingleton<IStartupService, StartupService>();
                        services.AddSingleton<IDialogService, DialogService>(); // branded dialog

                        // Typed HttpClient + Polly for IApiClient
                        services.AddHttpClient<IApiClient, ApiClient>()
                            .ConfigureHttpClient((sp, client) =>
                            {
                                var s = sp.GetRequiredService<ISettingsService>().Load();
                                client.BaseAddress = new Uri(s.ApiBaseUrl ?? "https://sigstreamcloud.com");
                                client.Timeout = TimeSpan.FromSeconds(15);
                            })
                            .AddPolicyHandler(GetRetryPolicy())
                            .AddPolicyHandler(GetCircuitBreakerPolicy());

                        // Background offline-queue flush
                        services.AddHostedService<QueueFlushService>();

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

                // Warm VMs/services that must run even if their pages aren't opened
                _ = HostInstance.Services.GetRequiredService<ViewModels.ComPortViewModel>();

                // Apply “Start with Windows”
                var settingsSvc = HostInstance.Services.GetRequiredService<ISettingsService>();
                var settings = settingsSvc.Load();
                var startup = HostInstance.Services.GetRequiredService<IStartupService>();
                startup.SetEnabled(settings.AutoStartOnBoot);

                // Show main window (optionally minimized to tray)
                var main = HostInstance.Services.GetRequiredService<Views.MainWindow>();
                if (settings.StartMinimized)
                {
                    main.ShowActivated = false;
                    main.WindowState = WindowState.Minimized;
                    main.ShowInTaskbar = false;
                    main.Show();
                    main.Hide(); // minimize to tray without flashing
                }
                else
                {
                    main.Show();
                }
            }
            catch
            {
                ShowError("SigStream Agent failed to start.", "Startup error");
                Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            try
            {
                if (HostInstance != null)
                {
                    await HostInstance.StopAsync();
                    HostInstance.Dispose();
                }
            }
            catch
            {
                // swallow on shutdown
            }

            _singleInstance?.ReleaseMutex();
            _singleInstance?.Dispose();

            base.OnExit(e);
        }

        // Tray context menu handlers (wired by TrayService to these names)
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
            var hb = HostInstance.Services.GetRequiredService<IHeartbeatService>();
            hb.Stop();
        }

        private void TrayPause_Unchecked(object sender, RoutedEventArgs e)
        {
            var s = HostInstance.Services.GetRequiredService<ISettingsService>().Load();
            var hb = HostInstance.Services.GetRequiredService<IHeartbeatService>();
            hb.Start(s);
        }

        // --- Polly policies ---
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy() =>
            HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(
                    retryCount: 5,
                    sleepDurationProvider: attempt =>
                        TimeSpan.FromSeconds(Math.Min(0.5 * Math.Pow(2, attempt - 1), 15))
                        + TimeSpan.FromMilliseconds(Random.Shared.Next(0, 250)));

        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy() =>
            HttpPolicyExtensions.HandleTransientHttpError()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 8,
                    durationOfBreak: TimeSpan.FromSeconds(60));

        // ========= Helpers: Branded dialogs with safe fallback =========
        private static IDialogService? ResolveDialogService()
        {
            try
            {
                return HostInstance?.Services.GetService<IDialogService>();
            }
            catch { return null; }
        }

        private static void ShowInfo(string message, string header = "Info")
        {
            var dlg = ResolveDialogService();
            if (dlg != null) { dlg.Info(message, header); return; }
            MessageBox.Show(message, "SigStream", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private static void ShowError(string message, string header = "Error")
        {
            var dlg = ResolveDialogService();
            if (dlg != null) { dlg.Error(message, header); return; }
            MessageBox.Show(message, "SigStream", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
