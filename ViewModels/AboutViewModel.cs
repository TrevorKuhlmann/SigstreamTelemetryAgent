// ViewModels/AboutViewModel.cs
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

            // hydrate from sources of truth
            var s = _settings.Load();
            _startWithWindows = _startup.IsEnabled();   // actual Run key
            _startMinimized = s.StartMinimized;

            // reflect external mutations
            _settings.SettingsChanged += (_, __) =>
            {
                var ss = _settings.Load();
                _startWithWindows = _startup.IsEnabled();
                _startMinimized = ss.StartMinimized;

                OnPropertyChanged(nameof(StartWithWindows));
                OnPropertyChanged(nameof(StartMinimized));
            };
        }

        private bool _startWithWindows;
        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (_startWithWindows == value) return;
                _startWithWindows = value;
                OnPropertyChanged();

                _startup.SetEnabled(value);        // apply Run key now
                var s = _settings.Load();
                s.AutoStartOnBoot = value;         // persist flag too
                _settings.Save(s);
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
                OnPropertyChanged();

                var s = _settings.Load();
                s.StartMinimized = value;          // persist immediately
                _settings.Save(s);
            }
        }
    }
}
