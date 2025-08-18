// ViewModels/MainWindowViewModel.cs
using Microsoft.Extensions.DependencyInjection;
using SigstreamTelemetryAgent.Models;
using SigstreamTelemetryAgent.Services;
using SigstreamTelemetryAgent.Helpers;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media; // Brush/Brushes
using System.Threading.Tasks;

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

        // Header/status bindables
        public string RegistrationStatusText => IsRegistered ? "Registered" : "Not registered";
        public Brush RegistrationStatusBrush => IsRegistered ? Brushes.Green : Brushes.Red;

        public ICommand NavigateDashboard { get; }
        public ICommand NavigateComPort { get; }
        public ICommand NavigateApiConfig { get; }
        public ICommand NavigateAbout { get; }

        public event EventHandler? RegistrationChanged;

        public void RaiseRegistrationChanged()
        {
            RegistrationChanged?.Invoke(this, System.EventArgs.Empty);
            OnPropertyChanged(nameof(IsRegistered));
            OnPropertyChanged(nameof(RegistrationStatusText));
            OnPropertyChanged(nameof(RegistrationStatusBrush));
        }

        public MainWindowViewModel(ISettingsService settings, IApiClient api, IHeartbeatService heartbeat, ITrayService tray)
        {
            _settings = settings; _api = api; _heartbeat = heartbeat; _tray = tray;
            Settings = _settings.Load();

            NavigateDashboard = new RelayCommand(_ => NavigateTo<Views.Pages.DashboardPage>());
            NavigateComPort = new RelayCommand(_ => NavigateTo<Views.Pages.ComPortPage>());
            NavigateApiConfig = new RelayCommand(_ => NavigateTo<Views.Pages.ApiConfigPage>());
            NavigateAbout = new RelayCommand(_ => NavigateTo<Views.Pages.AboutPage>());

            // On server revocation → stop, clear UI, jump to API config
            _heartbeat.RevokedChanged += async (_, revoked) =>
            {
                if (revoked) await HandleRevocationAsync("Server reported revocation.");
            };

            if (IsRegistered) _ = CheckRegistrationAndStartAsync();
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

        public async Task HandleRevocationAsync(string reason)
        {
            // Ensure all UI-bound changes happen on the WPF UI thread
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                // stop heartbeats first
                _heartbeat.Stop();

                // clear credentials and persist
                Settings.ApiKey = null;
                Settings.MachineId = null;
                Settings.SendHeartbeats = false;
                _settings.Save(Settings);

                // toast + notify + refresh header bindings
                _tray.ShowInfoToast($"Access revoked: {reason}");
                RaiseRegistrationChanged();

                // navigate user to API config so they can re-register immediately
                NavigateApiConfig?.Execute(null);
            });
        }
    }
}
