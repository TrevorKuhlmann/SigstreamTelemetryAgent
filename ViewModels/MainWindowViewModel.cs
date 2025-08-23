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
        public bool InputsEnabled => !IsRegistered;
        public AppSettings Settings { get; }
        public bool IsRegistered => !string.IsNullOrWhiteSpace(Settings.ApiKey) && !string.IsNullOrWhiteSpace(Settings.MachineId);

        // Header/status bindables (also used by sidebar status line)
        public string RegistrationStatusText => IsRegistered ? "Registered" : "Not registered";
        public Brush RegistrationStatusBrush => IsRegistered ? Brushes.Green : Brushes.Red;

        public ICommand NavigateDashboard { get; }
        public ICommand NavigateComPort { get; }
        public ICommand NavigateApiConfig { get; }
        public ICommand NavigateAbout { get; }


        private bool _isDashboardActive, _isComPortActive, _isApiConfigActive, _isAboutActive;
        public bool IsDashboardActive { get => _isDashboardActive; set { _isDashboardActive = value; OnPropertyChanged(); } }
        public bool IsComPortActive { get => _isComPortActive; set { _isComPortActive = value; OnPropertyChanged(); } }
        public bool IsApiConfigActive { get => _isApiConfigActive; set { _isApiConfigActive = value; OnPropertyChanged(); } }
        public bool IsAboutActive { get => _isAboutActive; set { _isAboutActive = value; OnPropertyChanged(); } }


        public event EventHandler? RegistrationChanged;


        private void SetActive(string pageName)
        {
            IsDashboardActive = pageName == nameof(Views.Pages.DashboardPage);
            IsComPortActive = pageName == nameof(Views.Pages.ComPortPage);
            IsApiConfigActive = pageName == nameof(Views.Pages.ApiConfigPage);
            IsAboutActive = pageName == nameof(Views.Pages.AboutPage);
        }


        public MainWindowViewModel(ISettingsService settings, IApiClient api, IHeartbeatService heartbeat, ITrayService tray)
        {
            _settings = settings;
            _api = api;
            _heartbeat = heartbeat;
            _tray = tray;

            Settings = _settings.Load();

            // 🔗 React to ANY settings mutation (register/revoke/label/heartbeat toggles)
            // This ensures UI flips immediately without restarting the app.
            _settings.SettingsChanged += (_, __) =>
            {
                // Start/stop heartbeat based on current registration state
                if (IsRegistered)
                {
                    // Safe-start (idempotent) — starts only if not already running
                    _heartbeat.Start(Settings);
                }
                else
                {
                    _heartbeat.Stop();
                }

                // Notify UI + commands
                RaiseRegistrationChanged();
            };

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
            SetActive(typeof(T).Name);
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
                OnPropertyChanged(nameof(InputsEnabled)); // <-- add this

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
                // Stop heartbeat first to prevent races
                _heartbeat.Stop();

                // Clear credentials and persist
                Settings.ApiKey = null;
                Settings.MachineId = null;
                Settings.SendHeartbeats = false;
                _settings.Save(Settings); // triggers SettingsChanged → UI updates

                _tray.ShowInfoToast($"Access revoked: {reason}");

                // Extra safety (SettingsChanged also calls this)
                RaiseRegistrationChanged();

                // Return user to API Config so inputs are enabled
                NavigateApiConfig?.Execute(null);
            }

            if (disp?.CheckAccess() == true) DoRevoke();
            else disp?.Invoke(DoRevoke);

            return Task.CompletedTask;
        }
    }
}
