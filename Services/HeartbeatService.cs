// Services/HeartbeatService.cs
using System;
using System.Threading;
using System.Threading.Tasks;

// Use the timers we actually want:
using Timer = System.Timers.Timer;
using ElapsedEventArgs = System.Timers.ElapsedEventArgs;

namespace SigstreamTelemetryAgent.Services
{
    public sealed class HeartbeatEventArgs : EventArgs
    {
        public DateTime Utc { get; }
        public bool Success { get; }
        public HeartbeatEventArgs(DateTime utc, bool success) { Utc = utc; Success = success; }
    }

    public class HeartbeatService : IHeartbeatService
    {
        private readonly IApiClient _api;
        private readonly ISettingsService _settings;
        private Timer? _timer;
        private Models.AppSettings? _current;          // for the elapsed handler
        private readonly SemaphoreSlim _gate = new(1, 1);

        public event EventHandler<bool>? RevokedChanged;
        public event EventHandler<HeartbeatEventArgs>? Beat;

        public HeartbeatService(IApiClient api, ISettingsService settings)
        { _api = api; _settings = settings; }

        public void Start(Models.AppSettings s)
        {
            Stop();
            if (!s.SendHeartbeats || string.IsNullOrWhiteSpace(s.ApiKey) || string.IsNullOrWhiteSpace(s.MachineId)) return;

            _current = s;
            _timer = new Timer(Math.Max(5, s.HeartbeatSeconds) * 1000);
            _timer.Elapsed += OnTimerElapsed;          // named handler = easy unsubscribe
            _timer.AutoReset = true;
            _timer.Start();

            _ = TickAsync(s);                           // immediate first beat
        }

        public void Stop()
        {
            if (_timer != null)
            {
                _timer.Elapsed -= OnTimerElapsed;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }
        }

        private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
        {
            if (_current == null) return;
            if (!await _gate.WaitAsync(0)) return;      // skip if a tick is still running
            try { await TickAsync(_current); }
            catch { /* swallow to avoid crashing the timer thread */ }
            finally { _gate.Release(); }
        }

        private async Task TickAsync(Models.AppSettings s)
        {
            var status = await _api.CheckStatusAsync(s.ApiKey!, s.MachineId!);
            if (status?.IsRevoked == true)
            {
                RevokedChanged?.Invoke(this, true);
                Stop();
                return;
            }

            var ok = await _api.SendHeartbeatAsync(s.ApiKey!, s.MachineId!);
            Beat?.Invoke(this, new HeartbeatEventArgs(DateTime.UtcNow, ok));
        }
    }
}
