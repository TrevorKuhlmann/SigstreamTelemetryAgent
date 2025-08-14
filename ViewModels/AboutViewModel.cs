// ViewModels/AboutViewModel.cs
using SigstreamTelemetryAgent.Services;
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class AboutViewModel : BaseViewModel
    {
        private readonly ISettingsService _settings;
        private readonly IStartupService _startup;

        public AboutViewModel(ISettingsService settings, IStartupService startup)
        {
            _settings = settings; _startup = startup;
            var s = _settings.Load();
            _startWithWindows = _startup.IsEnabled();        // authoritative
            _startMinimized = s.StartMinimized;
        }

        private bool _startWithWindows;
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (_startWithWindows == value) return;
                _startWithWindows = value;
                _startup.SetEnabled(value);
                var s = _settings.Load(); s.AutoStartOnBoot = value; _settings.Save(s);
                OnPropertyChanged();
            }
        }

        private bool _startMinimized;
        public bool StartMinimized
        {
            get => _startMinimized;
            set
            {
                if (_startMinimized == value) return;
                _startMinimized = value;
                var s = _settings.Load(); s.StartMinimized = value; _settings.Save(s);
                OnPropertyChanged();
            }
        }
    }
}
