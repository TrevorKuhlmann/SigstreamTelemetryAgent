using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services
{
    public class HeartbeatService : IHeartbeatService
    {
        private readonly IApiClient _api;
        private readonly ISettingsService _settings;
        private readonly IToastService _toast;

        private Timer? _hbTimer;
        private Timer? _probeTimer;
        private volatile bool _running;

        // events expected by your app
        public event EventHandler<HeartbeatEventArgs>? Beat;
        public event EventHandler<bool>? RevokedChanged;

        // how often to probe for revocation (seconds)
        private const int ProbeSeconds = 5;     // ← snappy; adjust if you like

        public HeartbeatService(IApiClient api, ISettingsService settings, IToastService toast)
        {
            _api = api;
            _settings = settings;
            _toast = toast;
        }

        public void Start(AppSettings app)
        {
            Stop(); // reset
            _running = true;

            var hbMs = Math.Max(5, app.HeartbeatSeconds) * 1000;

            _hbTimer = new Timer(async _ => await HeartbeatTick().ConfigureAwait(false), null, 1000, hbMs);
            _probeTimer = new Timer(async _ => await ProbeTick().ConfigureAwait(false), null, 2000, ProbeSeconds * 1000);
        }

        public void Stop()
        {
            _running = false;
            _hbTimer?.Dispose(); _hbTimer = null;
            _probeTimer?.Dispose(); _probeTimer = null;
        }

        private async Task HeartbeatTick()
        {
            if (!_running) return;

            var app = _settings.Load();
            if (string.IsNullOrWhiteSpace(app?.ApiKey) || string.IsNullOrWhiteSpace(app?.MachineId))
                return;

            // Optional dev pre-check
            var status = await _api.CheckStatusAsync(app.ApiKey!, app.MachineId!);
            if (status?.IsRevoked == true)
            {
                HandleRevoked();
                return;
            }

            var ok = await _api.SendHeartbeatAsync(app.ApiKey!, app.MachineId!);
            var code = (_api as ApiClient)?.LastStatusCode;

            try { Beat?.Invoke(this, new HeartbeatEventArgs(DateTime.UtcNow, ok, code)); } catch { /* ignore */ }

            if (!ok && (code == HttpStatusCode.Unauthorized || code == HttpStatusCode.Forbidden))
            {
                HandleRevoked();
            }
        }

        private async Task ProbeTick()
        {
            if (!_running) return;

            var app = _settings.Load();
            if (string.IsNullOrWhiteSpace(app?.ApiKey) || string.IsNullOrWhiteSpace(app?.MachineId))
                return;

            var active = await (_api as ApiClient)?.ProbeKeyAsync(app.ApiKey!, app.MachineId!)!;
            if (active == false)
            {
                HandleRevoked();
            }
            // null => network hiccup; ignore
        }

        private void HandleRevoked()
        {
            if (!_running) return;

            var app = _settings.Load() ?? new AppSettings();
            app.SendHeartbeats = false;
            app.ApiKey = null;
            _settings.Save(app);

            try { _toast?.ShowSuccess("API key revoked by server. Heartbeats stopped."); } catch { }
            try { RevokedChanged?.Invoke(this, true); } catch { }

            Stop();
        }
    }
}
