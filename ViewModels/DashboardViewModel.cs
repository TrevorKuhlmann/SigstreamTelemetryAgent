// ViewModels/DashboardViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using SigstreamTelemetryAgent.Services;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class DashboardViewModel : BaseViewModel
    {
        private readonly MainWindowViewModel _main;
        private readonly IHeartbeatService _heartbeat;

        public DashboardViewModel(MainWindowViewModel main, IHeartbeatService heartbeat)
        {
            _main = main;
            _heartbeat = heartbeat;

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
        }

        // Status passthrough
        public bool IsRegistered => _main.IsRegistered;
        public string RegistrationStatusText => _main.RegistrationStatusText;
        public Brush RegistrationStatusBrush => _main.RegistrationStatusBrush;

        // Heartbeats
        private int _heartbeatCounter;
        public int HeartbeatCounter
        {
            get => _heartbeatCounter;
            private set { if (_heartbeatCounter != value) { _heartbeatCounter = value; OnPropertyChanged(); } }
        }

        // Activity feed
        public ObservableCollection<ActivityItem> Activity { get; } = new();
        private const int MaxItems = 100;

        private void AddActivity(string message, ActivityLevel level = ActivityLevel.Info)
        {
            Activity.Add(new ActivityItem(DateTime.Now, message, level));
            while (Activity.Count > MaxItems) Activity.RemoveAt(0);
            OnPropertyChanged(nameof(Activity));
        }

        private void NotifyStatus()
        {
            OnPropertyChanged(nameof(IsRegistered));
            OnPropertyChanged(nameof(RegistrationStatusText));
            OnPropertyChanged(nameof(RegistrationStatusBrush));
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
