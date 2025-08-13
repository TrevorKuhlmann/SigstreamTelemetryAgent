// ViewModels/ApiConfigViewModel.cs
using SigstreamTelemetryAgent.Services;
using SigstreamTelemetryAgent.Helpers;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class ApiConfigViewModel : BaseViewModel
    {
        private readonly MainWindowViewModel _main;
        private readonly IApiClient _api; private readonly ISettingsService _settings; private readonly IToastService _toast; private readonly IOfflineQueue _queue;
        public string ApiKey { get; set; } = string.Empty;
        public string DeviceLabel { get; set; } = string.Empty;
        public bool InputsEnabled => !_main.IsRegistered;
        public AsyncCommand RegisterCommand { get; }

        public ApiConfigViewModel(MainWindowViewModel main, IApiClient api, ISettingsService settings, IToastService toast, IOfflineQueue queue)
        {
            _main = main; _api = api; _settings = settings; _toast = toast; _queue = queue;
            _main.RegistrationChanged += (_, __) => { OnPropertyChanged(nameof(InputsEnabled)); };
            RegisterCommand = new AsyncCommand(RegisterAsync);
        }

        private async Task RegisterAsync()
        {
            var res = await _api.RegisterAsync(ApiKey.Trim(), DeviceLabel.Trim());
            if (!res.Success || string.IsNullOrWhiteSpace(res.MachineId)) { _toast.ShowError("Registration failed. Check your key."); return; }
            var s = _settings.Load(); s.ApiKey = ApiKey.Trim(); s.MachineId = res.MachineId; s.DeviceLabel = DeviceLabel.Trim(); _settings.Save(s);
            _toast.ShowSuccess("Registered successfully.");
            await _queue.FlushAsync(s.ApiKey!, s.MachineId!, rec => _api.SendDataAsync(s.ApiKey!, s.MachineId!, rec));
            _main.RaiseRegistrationChanged();
        }
    }
}