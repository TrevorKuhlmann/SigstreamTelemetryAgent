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

        public DashboardViewModel(MainWindowViewModel main, IHeartbeatService heartbeat, IOfflineQueue queue, IApiClient api)
        {
            _main = main;
            _heartbeat = heartbeat;
            _queue = queue;
            _api = api;

            // Live beats → increment + log
            _heartbeat.Beat += (_, e) => OnUI(() =>
            {
                HeartbeatCounter = e.Ok ? HeartbeatCounter + 1 : HeartbeatCounter;
                AddActivity(e.Ok ? "Heartbeat OK" : $"Heartbeat failed {(int?)e.StatusCode}", e.Ok ? ActivityLevel.Info : ActivityLevel.Warn);
            });

            // Server revoke → zero counter + log
            _heartbeat.RevokedChanged += (_, __) => OnUI(() =>
            {
                HeartbeatCounter = 0;
                AddActivity("Access revoked by server", ActivityLevel.Error);
                NotifyStatus();
            });

            // Register/revoke from anywhere in app
            _main.RegistrationChanged += (_, __) => OnUI(() =>
            {
                if (!IsRegistered) HeartbeatCounter = 0;
                AddActivity(IsRegistered ? "Registered" : "Not registered", ActivityLevel.Info);
                NotifyStatus();
            });

            // Offline queue stats
            _queue.StatsChanged += (_, __) => OnUI(() =>
            {
                OnPropertyChanged(nameof(QueueDepth));
                OnPropertyChanged(nameof(QueueSizeText));
            });
            _queue.Flushed += (_, e) => OnUI(() =>
            {
                AddActivity($"Flushed {e.Sent} queued item(s), {e.Remaining} remaining", ActivityLevel.Info);
                OnPropertyChanged(nameof(QueueDepth));
                OnPropertyChanged(nameof(QueueSizeText));
            });

            // one shot initial
            OnPropertyChanged(nameof(QueueDepth));
            OnPropertyChanged(nameof(QueueSizeText));

            FlushNow = new RelayCommand(async _ =>
            {
                var s = _main.Settings;
                if (string.IsNullOrWhiteSpace(s.ApiKey) || string.IsNullOrWhiteSpace(s.MachineId)) return;
                await _queue.FlushAsync(s.ApiKey!, s.MachineId!, rec => _api.SendDataAsync(s.ApiKey!, s.MachineId!, rec));
            });
        }

        // Status passthrough (and MachineId for the XAML)
        public bool IsRegistered => _main.IsRegistered;
        public string RegistrationStatusText => _main.RegistrationStatusText;
        public Brush RegistrationStatusBrush => _main.RegistrationStatusBrush;
        public string MachineId => _main.Settings?.MachineId ?? string.Empty;

        // Heartbeats
        private int _heartbeatCounter;
        public int HeartbeatCounter
        {
            get => _heartbeatCounter;
            private set { if (_heartbeatCounter != value) { _heartbeatCounter = value; OnPropertyChanged(); } }
        }

        // Offline queue bindables
        public int QueueDepth => _queue.Count;
        public string QueueSizeText => $"{Math.Max(0, _queue.SizeBytes) / (1024.0 * 1024.0):0.00} MB";
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
            OnPropertyChanged(nameof(MachineId));
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
