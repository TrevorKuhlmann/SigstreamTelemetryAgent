// Services/HeartbeatService.cs
namespace SigstreamTelemetryAgent.Services
{
    public class HeartbeatService : IHeartbeatService
    {
        private readonly IApiClient _api; private readonly ISettingsService _settings; private System.Timers.Timer? _timer;
        public event EventHandler<bool>? RevokedChanged;

        public HeartbeatService(IApiClient api, ISettingsService settings)
        { _api = api; _settings = settings; }

        public void Start(Models.AppSettings s)
        {
            Stop();
            if (!s.SendHeartbeats || string.IsNullOrWhiteSpace(s.ApiKey) || string.IsNullOrWhiteSpace(s.MachineId)) return;
            _timer = new System.Timers.Timer(Math.Max(5, s.HeartbeatSeconds) * 1000);
            _timer.Elapsed += async (_, __) => await TickAsync(s);
            _timer.AutoReset = true;
            _timer.Start();
            _ = TickAsync(s); // immediate first beat
        }

        public void Stop() { _timer?.Stop(); _timer?.Dispose(); _timer = null; }

        private async Task TickAsync(Models.AppSettings s)
        {
            var status = await _api.CheckStatusAsync(s.ApiKey!, s.MachineId!);
            if (status?.IsRevoked == true)
            {
                RevokedChanged?.Invoke(this, true);
                Stop();
                return;
            }
            await _api.SendHeartbeatAsync(s.ApiKey!, s.MachineId!);
        }
    }
}