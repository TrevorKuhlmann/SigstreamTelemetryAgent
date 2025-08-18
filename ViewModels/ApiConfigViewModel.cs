using System.Threading.Tasks;
using System.Windows;               // for Application.Current.Dispatcher
using System.Windows.Input;
using SigstreamTelemetryAgent.Services;
using SigstreamTelemetryAgent.Helpers;
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class ApiConfigViewModel : BaseViewModel
    {
        private readonly MainWindowViewModel _main;
        private readonly IApiClient _api;
        private readonly ISettingsService _settings;
        private readonly IToastService _toast;
        private readonly IHeartbeatService _heartbeat;
        private readonly IOfflineQueue _queue;

        public ApiConfigViewModel(
            MainWindowViewModel main,
            IApiClient api,
            ISettingsService settings,
            IToastService toast,
            IHeartbeatService heartbeat,
            IOfflineQueue queue)
        {
            _main = main; _api = api; _settings = settings; _toast = toast; _heartbeat = heartbeat; _queue = queue;

            // initial state
            SyncFromSettings();

            // when Main says registration changed (e.g., other pages updated), reflect it here
            _main.RegistrationChanged += (_, __) => SyncFromSettings();

            // when heartbeat detects server revocation → reset immediately
            _heartbeat.RevokedChanged += (_, __) =>
            {
                // Clear UI & enable inputs immediately
                ResetToDefaultUi();
                _toast.ShowInfo("API key revoked. Please register again.");
                _main.RaiseRegistrationChanged();
            };

            RegisterCommand = new AsyncCommand(RegisterAsync, () => !IsRegistered);
            RevokeCommand = new RelayCommand(_ => Revoke(), _ => IsRegistered);
            SaveSettings = new RelayCommand(_ => ApplySettings());
        }

        // Bindables
        public string ApiKey { get => _apiKey; set { _apiKey = value; OnPropertyChanged(); } }
        private string _apiKey = string.Empty;

        public string DeviceLabel { get => _deviceLabel; set { _deviceLabel = value; OnPropertyChanged(); } }
        private string _deviceLabel = string.Empty;

        public string MachineId { get => _machineId; private set { _machineId = value; OnPropertyChanged(); } }
        private string _machineId = string.Empty;

        public bool SendHeartbeats { get => _send; set { _send = value; OnPropertyChanged(); } }
        private bool _send;

        public int HeartbeatSeconds { get => _hb; set { _hb = value; OnPropertyChanged(); } }
        private int _hb = 30;

        public bool IsRegistered
        {
            get
            {
                var s = _settings.Load();
                return !string.IsNullOrWhiteSpace(s.ApiKey) && !string.IsNullOrWhiteSpace(s.MachineId);
            }
        }

        public bool InputsEnabled => !IsRegistered;

        // Commands
        public ICommand RegisterCommand { get; }
        public ICommand RevokeCommand { get; }
        public ICommand SaveSettings { get; }

        // ---------------- helpers ----------------

        private void SyncFromSettings()
        {
            var s = _settings.Load();
            ApiKey = s.ApiKey ?? string.Empty;
            DeviceLabel = s.DeviceLabel ?? string.Empty;
            MachineId = s.MachineId ?? string.Empty;
            SendHeartbeats = s.SendHeartbeats;
            HeartbeatSeconds = s.HeartbeatSeconds;

            OnPropertyChanged(nameof(IsRegistered));
            OnPropertyChanged(nameof(InputsEnabled));
            RequeryCommands();
        }

        private void ResetToDefaultUi()
        {
            // Clear local fields to base state
            ApiKey = string.Empty;
            MachineId = string.Empty;
            SendHeartbeats = false;

            OnPropertyChanged(nameof(IsRegistered));
            OnPropertyChanged(nameof(InputsEnabled));
            RequeryCommands();
        }

        private static void RequeryCommands()
        {
            // Ensure buttons (Register/Revoke) re-evaluate CanExecute immediately
            try
            {
                Application.Current?.Dispatcher?.Invoke(CommandManager.InvalidateRequerySuggested);
            }
            catch
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }

        // ---------------- actions ----------------

        private async Task RegisterAsync()
        {
            var key = (ApiKey ?? string.Empty).Trim();
            var label = (DeviceLabel ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                _toast.ShowError("Enter your API key.");
                return;
            }

            var res = await _api.RegisterAsync(key, label);
            if (!res.Success || string.IsNullOrWhiteSpace(res.MachineId))
            {
                _toast.ShowError(res.Error ?? "Registration failed.");
                return;
            }

            var s = _settings.Load();
            s.ApiKey = key;
            s.DeviceLabel = label;
            s.MachineId = res.MachineId;
            _settings.Save(s);

            MachineId = s.MachineId!;
            _toast.ShowSuccess("Registered successfully.");
            _main.RaiseRegistrationChanged();   // notify all pages/header

            _heartbeat.Start(s);                // start heartbeat with latest settings

            // try flush queued telemetry
            await _queue.FlushAsync(s.ApiKey!, s.MachineId!, rec => _api.SendDataAsync(s.ApiKey!, s.MachineId!, rec));

            // local UI refresh (enables/disables buttons)
            SyncFromSettings();
        }

        private void Revoke()
        {
            var s = _settings.Load();
            s.ApiKey = null;
            s.MachineId = null;
            s.SendHeartbeats = false;
            _settings.Save(s);

            _heartbeat.Stop();
            ResetToDefaultUi();
            _toast.ShowInfo("Credentials cleared. Inputs are re-enabled.");
            _main.RaiseRegistrationChanged();
        }

        private void ApplySettings()
        {
            var app = _settings.Load() ?? new AppSettings();

            app.DeviceLabel = this.DeviceLabel;
            app.SendHeartbeats = this.SendHeartbeats;
            app.HeartbeatSeconds = this.HeartbeatSeconds;

            // Intentionally NOT persisting ApiKey here (matches your current behavior)
            _settings.Save(app);

            if (app.SendHeartbeats && !string.IsNullOrWhiteSpace(app.ApiKey) && !string.IsNullOrWhiteSpace(app.MachineId))
                _heartbeat.Start(app);
            else
                _heartbeat.Stop();

            _toast.ShowSuccess("Settings saved");
            // reflect current state in header / other pages
            _main.RaiseRegistrationChanged();
        }
    }
}
