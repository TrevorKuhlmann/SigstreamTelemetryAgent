// ViewModels/ApiConfigViewModel.cs
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SigstreamTelemetryAgent.Services;
using SigstreamTelemetryAgent.Helpers;
using SigstreamTelemetryAgent.Models;
using System.Windows.Media;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class ApiConfigViewModel : BaseViewModel
    {

        public string RegistrationStatusText => _main.RegistrationStatusText;
        public System.Windows.Media.Brush RegistrationStatusBrush => _main.RegistrationStatusBrush;


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
            _main = main;
            _api = api;
            _settings = settings;
            _toast = toast;
            _heartbeat = heartbeat;
            _queue = queue;

            var s = _settings.Load();
            ApiKey = s.ApiKey ?? string.Empty;
            DeviceLabel = s.DeviceLabel ?? string.Empty;
            MachineId = s.MachineId ?? string.Empty;
            SendHeartbeats = s.SendHeartbeats;
            HeartbeatSeconds = s.HeartbeatSeconds;

            // Commands

            RegisterCommand = new AsyncCommand(RegisterAsync, CanRegister);
            RevokeCommand = new RelayCommand(_ => Revoke(), _ => IsRegistered); // harmless to keep
            SaveSettings = new RelayCommand(_ => ApplySettings());

            // Keep this page instantly in sync with app-level registration changes
            _main.RegistrationChanged += (_, __) =>
            {
                var ss = _settings.Load();
                MachineId = ss.MachineId ?? string.Empty;

                OnPropertyChanged(nameof(IsRegistered));
                OnPropertyChanged(nameof(InputsEnabled));

                OnPropertyChanged(nameof(RegistrationStatusText));   // <-- add
                OnPropertyChanged(nameof(RegistrationStatusBrush));  // <-- add

                RaiseCommandStates();
            };
        }

        // Bindables
        private string _apiKey = string.Empty;
        public string ApiKey
        {
            get => _apiKey;
            set
            {
                if (_apiKey != value)
                {
                    _apiKey = value;
                    OnPropertyChanged();
                    RaiseCommandStates(); // re-evaluate Register on text change
                }
            }
        }

        private string _deviceLabel = string.Empty;
        public string DeviceLabel
        {
            get => _deviceLabel;
            set { _deviceLabel = value; OnPropertyChanged(); }
        }

        private string _machineId = string.Empty;
        public string MachineId
        {
            get => _machineId;
            private set { _machineId = value; OnPropertyChanged(); }
        }

        private bool _send;
        public bool SendHeartbeats
        {
            get => _send;
            set { _send = value; OnPropertyChanged(); }
        }

        private int _hb = 30;
        public int HeartbeatSeconds
        {
            get => _hb;
            set { _hb = value; OnPropertyChanged(); }
        }

        // Source of truth from shell VM (keeps UI consistent)
        public bool IsRegistered => _main.IsRegistered;
        public bool InputsEnabled => _main.InputsEnabled;

        // Commands
        public ICommand RegisterCommand { get; }
        public ICommand RevokeCommand { get; }
        public ICommand SaveSettings { get; }

        private bool CanRegister() =>
            InputsEnabled && !string.IsNullOrWhiteSpace(ApiKey);

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

            // Notify shell + update command states immediately
            _main.RaiseRegistrationChanged();
            RaiseCommandStates();

            // Start heartbeat with latest settings
            _heartbeat.Start(s);

            // Try flush queued telemetry
            await _queue.FlushAsync(s.ApiKey!, s.MachineId!, rec => _api.SendDataAsync(s.ApiKey!, s.MachineId!, rec));
        }

        private void Revoke()
        {
            var s = _settings.Load();
            s.ApiKey = null;
            s.MachineId = null;
            _settings.Save(s);

            _heartbeat.Stop();
            _toast.ShowInfo("Credentials cleared. Inputs are re-enabled.");

            _main.RaiseRegistrationChanged();
            RaiseCommandStates();
        }

        private void ApplySettings()
        {
            var app = _settings.Load() ?? new AppSettings();

            // Persist label & heartbeat prefs
            app.DeviceLabel = this.DeviceLabel;
            app.SendHeartbeats = this.SendHeartbeats;

            // Optional clamp to avoid too-tight intervals
            app.HeartbeatSeconds = Math.Max(5, this.HeartbeatSeconds);

            // NOTE: ApiKey is only persisted on successful Register
            _settings.Save(app);

            if (app.SendHeartbeats)
            {
                _heartbeat.Start(app);
            }

            _toast.ShowSuccess("Settings saved");
        }

        private void RaiseCommandStates()
        {
            if (RegisterCommand is AsyncCommand ac) ac.RaiseCanExecuteChanged();
            if (RevokeCommand is RelayCommand rc) rc.RaiseCanExecuteChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
