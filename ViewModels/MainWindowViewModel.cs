// ViewModels/MainWindowViewModel.cs
using Microsoft.Extensions.DependencyInjection;
using SigstreamTelemetryAgent.Models;
using SigstreamTelemetryAgent.Services;
using SigstreamTelemetryAgent.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private readonly ISettingsService _settings;
        private readonly IApiClient _api;
        private readonly IHeartbeatService _heartbeat;
        private readonly ITrayService _tray;
        private Frame? _frame;

        public AppSettings Settings { get; }
        public bool IsRegistered => !string.IsNullOrWhiteSpace(Settings.ApiKey) && !string.IsNullOrWhiteSpace(Settings.MachineId);

        // Header/status bindables (also used by sidebar status line)
        public string RegistrationStatusText => IsRegistered ? "Registered" : "Not registered";
        public Brush RegistrationStatusBrush => IsRegistered ? Brushes.Green : Brushes.Red;

        public ICommand NavigateDashboard { get; }
        public ICommand NavigateComPort { get; }
        public ICommand NavigateApiConfig { get; }
        public ICommand NavigateAbout { get; }

        public event EventHandler? RegistrationChanged;

        public MainWindowViewModel(ISettingsService settings, IApiClient api, IHeartbeatService heartbeat, ITrayService tray)
        {
            _settings = settings;
            _api = api;
            _heartbeat = heartbeat;
            _tray = tray;

            Settings = _settings.Load();

            NavigateDashboard = new RelayCommand(_ => NavigateTo<Views.Pages.DashboardPage>());
            NavigateComPort = new RelayCommand(_ => NavigateTo<Views.Pages.ComPortPage>());
            NavigateApiConfig = new RelayCommand(_ => NavigateTo<Views.Pages.ApiConfigPage>());
            NavigateAbout = new RelayCommand(_ => NavigateTo<Views.Pages.AboutPage>());

            // Cloud-driven revocation → stop, clear, notify, navigate
            _heartbeat.RevokedChanged += async (_, revoked) =>
            {
                if (revoked) await HandleRevocationAsync("Server reported revocation.");
            };

            if (IsRegistered)
                _ = CheckRegistrationAndStartAsync();
        }

        public void Init(Frame frame)
        {
            _frame = frame;
            // Default page: API Config if not registered, else Dashboard
            if (IsRegistered) NavigateTo<Views.Pages.DashboardPage>();
            else NavigateTo<Views.Pages.ApiConfigPage>();
        }

        private void NavigateTo<T>() where T : Page
        {
            var page = (Page)App.HostInstance.Services.GetRequiredService(typeof(T));
            _frame!.Navigate(page);
        }

        private async Task CheckRegistrationAndStartAsync()
        {
            var status = await _api.CheckStatusAsync(Settings.ApiKey!, Settings.MachineId!);
            if (status?.IsRevoked == true)
            {
                await HandleRevocationAsync("API key is revoked on server.");
                return;
            }
            _heartbeat.Start(Settings);
        }

        public void RaiseRegistrationChanged()
        {
            void Fire()
            {
                OnPropertyChanged(nameof(IsRegistered));
                OnPropertyChanged(nameof(RegistrationStatusText));
                OnPropertyChanged(nameof(RegistrationStatusBrush));

                RegistrationChanged?.Invoke(this, EventArgs.Empty);

                // Re-evaluate all WPF commands (enables Register immediately)
                CommandManager.InvalidateRequerySuggested();
            }

            var disp = System.Windows.Application.Current?.Dispatcher;
            if (disp?.CheckAccess() == true) Fire();
            else disp?.Invoke(Fire);
        }

        public Task HandleRevocationAsync(string reason)
        {
            var disp = System.Windows.Application.Current?.Dispatcher;
            void DoRevoke()
            {
                _heartbeat.Stop();

                Settings.ApiKey = null;
                Settings.MachineId = null;
                Settings.SendHeartbeats = false;
                _settings.Save(Settings);

                _tray.ShowInfoToast($"Access revoked: {reason}");

                RaiseRegistrationChanged();
                NavigateApiConfig?.Execute(null);
            }

            if (disp?.CheckAccess() == true) DoRevoke();
            else disp?.Invoke(DoRevoke);

            return Task.CompletedTask;
        }
    }
}
