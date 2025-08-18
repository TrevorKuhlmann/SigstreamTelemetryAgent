using System;
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

            // Update counter on each beat (UI-thread safe)
            _heartbeat.Beat += (_, e) =>
                OnUI(() => HeartbeatCounter = e.Ok ? HeartbeatCounter + 1 : HeartbeatCounter);

            // If service reports revoked, zero the counter (MainWindow will also revoke)
            _heartbeat.RevokedChanged += (_, __) =>
                OnUI(() =>
                {
                    HeartbeatCounter = 0;
                    NotifyStatus();
                });

            // When registration flips (register/revoke), refresh status + counter
            _main.RegistrationChanged += (_, __) =>
                OnUI(() =>
                {
                    HeartbeatCounter = IsRegistered ? HeartbeatCounter : 0;
                    NotifyStatus();
                });
        }

        // ——— Bindables ———
        public bool IsRegistered => _main.IsRegistered;
        public string RegistrationStatusText => _main.RegistrationStatusText;
        public Brush RegistrationStatusBrush => _main.RegistrationStatusBrush;

        private int _heartbeatCounter;
        public int HeartbeatCounter
        {
            get => _heartbeatCounter;
            private set { if (_heartbeatCounter != value) { _heartbeatCounter = value; OnPropertyChanged(); } }
        }

        // ——— Helpers ———
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
}
