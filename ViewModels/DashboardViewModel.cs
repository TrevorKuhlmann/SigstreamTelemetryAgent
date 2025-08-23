// ViewModels/DashboardViewModel.cs
using SigstreamTelemetryAgent.Helpers;
using SigstreamTelemetryAgent.Services;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly MainWindowViewModel _main;
        private readonly IHeartbeatService _heartbeat;
        private readonly IOfflineQueue _queue;
        private readonly IApiClient _api;

        // --- Offline banner state ---
        private bool _isOffline;
        public bool IsOffline
        {
            get => _isOffline;
            private set { if (_isOffline != value) { _isOffline = value; OnPropertyChanged(); } }
        }

        private bool _lastBeatOk = true;

        public DashboardViewModel(MainWindowViewModel main, IHeartbeatService heartbeat, IOfflineQueue queue, IApiClient api)
        {
            _main = main;
            _heartbeat = heartbeat;
            _queue = queue;
            _api = api;

            _heartbeat.Beat += (_, e) => OnUI(() =>
            {
                _lastBeatOk = e.Ok;
                if (e.Ok) HeartbeatCounter++;
                AddActivity(e.Ok ? "Heartbeat OK" : $"Heartbeat failed {(int?)e.StatusCode}",
                            e.Ok ? ActivityLevel.Info : ActivityLevel.Warn);

                RecomputeOffline();
            });

            _heartbeat.RevokedChanged += (_, __) => OnUI(() =>
            {
                HeartbeatCounter = 0;
                _lastBeatOk = false;
                AddActivity("Access revoked by server", ActivityLevel.Error);
                NotifyStatus();
                RecomputeOffline();
            });

            _main.RegistrationChanged += (_, __) => OnUI(() =>
            {
                if (!IsRegistered) HeartbeatCounter = 0;
                AddActivity(IsRegistered ? "Registered" : "Not registered", ActivityLevel.Info);
                NotifyStatus();
                RecomputeOffline();
            });

            _queue.StatsChanged += (_, __) => OnUI(() =>
            {
                OnPropertyChanged(nameof(QueueDepth));
                OnPropertyChanged(nameof(QueueSizeBytes));
                OnPropertyChanged(nameof(QueueFillPercent));
                OnPropertyChanged(nameof(QueueFillBrush));
                OnPropertyChanged(nameof(QueueCapacityBytes));
                OnPropertyChanged(nameof(QueueStatusText));
                RecomputeOffline();
            });

            _queue.Flushed += (_, e) => OnUI(() =>
            {
                AddActivity($"Flushed {e.Sent} queued item(s), {e.Remaining} remaining", ActivityLevel.Info);
                OnPropertyChanged(nameof(QueueDepth));
                OnPropertyChanged(nameof(QueueSizeBytes));
                OnPropertyChanged(nameof(QueueFillPercent));
                OnPropertyChanged(nameof(QueueFillBrush));
                OnPropertyChanged(nameof(QueueCapacityBytes));
                OnPropertyChanged(nameof(QueueStatusText));
                RecomputeOffline();
            });

            // initial
            OnPropertyChanged(nameof(QueueDepth));
            OnPropertyChanged(nameof(QueueSizeBytes));
            OnPropertyChanged(nameof(QueueFillPercent));
            OnPropertyChanged(nameof(QueueFillBrush));
            OnPropertyChanged(nameof(QueueCapacityBytes));
            OnPropertyChanged(nameof(QueueStatusText));
            RecomputeOffline();

            FlushNow = new RelayCommand(async _ =>
            {
                var s = _main.Settings;
                if (string.IsNullOrWhiteSpace(s.ApiKey) || string.IsNullOrWhiteSpace(s.MachineId)) return;
                await _queue.FlushAsync(s.ApiKey!, s.MachineId!, rec => _api.SendDataAsync(s.ApiKey!, s.MachineId!, rec));
            });
        }

        private void RecomputeOffline()
        {
            // If you want the banner even while not registered, remove the IsRegistered check.
            if (!IsRegistered)
            {
                IsOffline = _queue.Count > 0; // show only if there's cached data while unregistered
                return;
            }

            IsOffline = !_lastBeatOk || _queue.Count > 0;
        }

        // Status passthrough
        public bool IsRegistered => _main.IsRegistered;
        public string RegistrationStatusText => _main.RegistrationStatusText;
        public Brush RegistrationStatusBrush => _main.RegistrationStatusBrush;
        public string DeviceLabel => _main.Settings?.DeviceLabel ?? string.Empty;

        public long QueueCapacityBytes => _queue.MaxCapacityBytes;
        public string QueueStatusText =>
            QueueFillPercent < 60 ? "healthy" :
            QueueFillPercent < 85 ? "filling" :
                                    "almost full";

        // Heartbeats
        private int _heartbeatCounter;
        public int HeartbeatCounter
        {
            get => _heartbeatCounter;
            private set { if (_heartbeatCounter != value) { _heartbeatCounter = value; OnPropertyChanged(); } }
        }

        // Offline queue bindables
        public int QueueDepth => _queue.Count;
        public long QueueSizeBytes => Math.Max(0, _queue.SizeBytes);
        public double QueueFillPercent =>
            (_queue is not null && _queue.MaxCapacityBytes > 0)
                ? Math.Min(100.0, (100.0 * _queue.SizeBytes) / _queue.MaxCapacityBytes)
                : 0.0;

        public Brush QueueFillBrush =>
            QueueFillPercent < 60 ? Brushes.SeaGreen :
            QueueFillPercent < 85 ? Brushes.Goldenrod :
                                    Brushes.IndianRed;

        public ICommand FlushNow { get; }

        // Activity feed
        public ObservableCollection<ActivityItem> Activity { get; } = new();

        private void AddActivity(string message, ActivityLevel level = ActivityLevel.Info)
        {
            Activity.Add(new ActivityItem(DateTime.Now, message, level));
            while (Activity.Count > 100) Activity.RemoveAt(0);
            OnPropertyChanged(nameof(Activity));
        }

        private void NotifyStatus()
        {
            OnPropertyChanged(nameof(IsRegistered));
            OnPropertyChanged(nameof(RegistrationStatusText));
            OnPropertyChanged(nameof(RegistrationStatusBrush));
            OnPropertyChanged(nameof(DeviceLabel));
        }

        private static void OnUI(Action a)
        {
            var d = Application.Current?.Dispatcher;
            if (d?.CheckAccess() == true) a(); else d?.Invoke(a);
        }
    }

    public enum ActivityLevel { Info, Warn, Error }
    public record ActivityItem(DateTime When, string Message, ActivityLevel Level);


}
