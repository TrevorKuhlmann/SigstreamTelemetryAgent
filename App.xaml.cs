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

        private static System.Threading.Mutex? _singleInstance;

        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(path: System.IO.Path.Combine(
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData),
                    "SigStream", "logs", "agent-.log"),
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();



            bool createdNew;
            _singleInstance = new System.Threading.Mutex(true, "SigstreamTelemetryAgent_Mutex", out createdNew);
            if (!createdNew)
            {
                // already running
                MessageBox.Show("SigStream Agent is already running.", "SigStream", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }


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

            _singleInstance?.ReleaseMutex();
            _singleInstance?.Dispose();
        }




        private void TrayOpen_Click(object sender, RoutedEventArgs e)
        {
            var win = HostInstance.Services.GetRequiredService<Views.MainWindow>();
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
            var settings = HostInstance.Services.GetRequiredService<Services.ISettingsService>().Load();
            var hb = HostInstance.Services.GetRequiredService<Services.IHeartbeatService>();
            hb.Start(settings);
        }
    }
}