// ViewModels/AboutViewModel.cs
using System.Diagnostics;
using SigstreamTelemetryAgent.Services;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        private readonly ISettingsService _settings;
        private readonly IStartupService _startup;

        public AboutViewModel(ISettingsService settings, IStartupService startup)
        {
            _settings = settings;
            _startup = startup;

            // hydrate
            var s = _settings.Load();
            _startWithWindows = _startup.IsEnabled();
            _startMinimized = s.StartMinimized;

            // reflect external changes
            _settings.SettingsChanged += (_, __) =>
            {
                var ss = _settings.Load();
                _startWithWindows = _startup.IsEnabled();
                _startMinimized = ss.StartMinimized;
                OnPropertyChanged(nameof(StartWithWindows));
                OnPropertyChanged(nameof(StartMinimized));
            };
        }

        // ——— Startup toggles ———
        private bool _startWithWindows;
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (_startWithWindows == value) return;
                _startWithWindows = value; OnPropertyChanged();
                _startup.SetEnabled(value);
                var s = _settings.Load(); s.AutoStartOnBoot = value; _settings.Save(s);
            }
        }

        private bool _startMinimized;
        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                if (_startMinimized == value) return;
                _startMinimized = value; OnPropertyChanged();
                var s = _settings.Load(); s.StartMinimized = value; _settings.Save(s);
            }
        }

        // ——— About info ———
        public string ProductName => "SigStream Agent";
        public string Version =>
            FileVersionInfo.GetVersionInfo(GetType().Assembly.Location).ProductVersion ?? "0.0.0";
        public string Website => "https://sigstreamcloud.com";
        public string SupportEmail => "admin@sigstreamcloud.com";
        public string Disclaimer =>
            "SigStream Agent sends telemetry you configure to SigStream Cloud. Use at your own risk.";
    }
}
