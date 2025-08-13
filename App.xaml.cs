// App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.Windows;

namespace SigstreamTelemetryAgent
{
    public partial class App : Application
    {
        public static IHost HostInstance { get; private set; } = default!;

        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(path: System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "SigStream", "logs", "agent-.log"),
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            HostInstance = Host.CreateDefaultBuilder()
                .UseSerilog()
                .ConfigureServices((ctx, services) =>
                {
                    // Services
                    services.AddSingleton<Services.ISettingsService, Services.SettingsService>();
                    services.AddSingleton<Services.IApiClient, Services.ApiClient>();
                    services.AddSingleton<Services.IHeartbeatService, Services.HeartbeatService>();
                    services.AddSingleton<Services.IComPortService, Services.ComPortService>();
                    services.AddSingleton<Services.IOfflineQueue, Services.OfflineQueue>();
                    services.AddSingleton<Services.IToastService, Services.ToastService>();
                    services.AddSingleton<Services.ITrayService, Services.TrayService>();

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

            var main = HostInstance.Services.GetRequiredService<Views.MainWindow>();
            main.Show();
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await HostInstance.StopAsync();
            HostInstance.Dispose();
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}