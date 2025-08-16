using Microsoft.Extensions.DependencyInjection;
using SigstreamTelemetryAgent.Helpers;
using SigstreamTelemetryAgent.Services;
using System;
using System.Windows.Input;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly ISettingsService _settings;
        private readonly IOfflineQueue _queue;
        private readonly IHeartbeatService _heartbeat;

        public DashboardViewModel(ISettingsService settings, IOfflineQueue queue, IHeartbeatService heartbeat)
        {
            _settings = settings; _queue = queue; _heartbeat = heartbeat;

            var s = _settings.Load();
            DeviceLabel = s.DeviceLabel ?? "";
            MachineId = s.MachineId ?? "";
            IsRegistered = !string.IsNullOrWhiteSpace(s.ApiKey) && !string.IsNullOrWhiteSpace(s.MachineId);
            QueueDepth = _queue.EstimateDepth();

            _heartbeat.Beat += (_, e) =>
            {
                LastHeartbeatUtc = e.Utc;
                LastHeartbeatOk = e.Success;
            };

            // light polling for queue depth + "x seconds ago" label
            _timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += (_, __) =>
            {
                OnPropertyChanged(nameof(LastHeartbeatAgo));
                // refresh depth every 3 ticks (~3s)
                if (++_tick % 3 == 0) QueueDepth = _queue.EstimateDepth();
            };
            _timer.Start();

            FlushNow = new AsyncCommand(async () =>
            {
                var st = _settings.Load();
                if (string.IsNullOrWhiteSpace(st.ApiKey) || string.IsNullOrWhiteSpace(st.MachineId)) return;
                await _queue.FlushAsync(st.ApiKey!, st.MachineId!, rec => _api.SendDataAsync(st.ApiKey!, st.MachineId!, rec));
                QueueDepth = _queue.EstimateDepth();
            });

            _api = App.HostInstance.Services.GetRequiredService<IApiClient>(); // resolve lazily to avoid cycles
        }

        private readonly System.Windows.Threading.DispatcherTimer _timer;
        private int _tick;
        private readonly IApiClient _api;

        // Registration
        private bool _isRegistered;
        public bool IsRegistered { get => _isRegistered; private set { _isRegistered = value; OnPropertyChanged(); } }

        public string DeviceLabel { get; }
        public string MachineId { get; }

        // Heartbeat
        private DateTime? _lastUtc;
        public DateTime? LastHeartbeatUtc { get => _lastUtc; private set { _lastUtc = value; OnPropertyChanged(); OnPropertyChanged(nameof(LastHeartbeatAgo)); } }

        private bool _lastOk;
        public bool LastHeartbeatOk { get => _lastOk; private set { _lastOk = value; OnPropertyChanged(); } }

        public string LastHeartbeatAgo =>
            LastHeartbeatUtc is null ? "—" :
            FormatAgo(DateTime.UtcNow - LastHeartbeatUtc.Value);

        private static string FormatAgo(TimeSpan ts)
        {
            if (ts.TotalSeconds < 1) return "just now";
            if (ts.TotalSeconds < 60) return $"{(int)ts.TotalSeconds}s ago";
            if (ts.TotalMinutes < 60) return $"{(int)ts.TotalMinutes}m ago";
            return $"{(int)ts.TotalHours}h ago";
        }

        // Queue
        private int _queueDepth;
        public int QueueDepth { get => _queueDepth; private set { _queueDepth = value; OnPropertyChanged(); } }

        // Actions
        public ICommand FlushNow { get; }
    }
}
