// ViewModels/ComPortViewModel.cs
using SigstreamTelemetryAgent.Services;
using SigstreamTelemetryAgent.Models;
using System.Collections.ObjectModel;
using SigstreamTelemetryAgent.Services;
namespace SigstreamTelemetryAgent.ViewModels
{
    public class ComPortViewModel : BaseViewModel
    {
        private readonly IComPortService _com; private readonly IApiClient _api; private readonly Services.ISettingsService _settings; private readonly IOfflineQueue _queue;
        public ObservableCollection<string> Ports { get; } = new();
        public ObservableCollection<string> RecentLines { get; } = new();

        public ComPortViewModel(IComPortService com, IApiClient api, Services.ISettingsService settings, IOfflineQueue queue)
        {
            _com = com; _api = api; _settings = settings; _queue = queue;
            foreach (var p in _com.GetPorts()) Ports.Add(p);
            _com.LineReceived += async (_, line) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RecentLines.Insert(0, line.Trim());
                    while (RecentLines.Count > 100) RecentLines.RemoveAt(RecentLines.Count - 1);
                });

                var record = new TelemetryRecord { Raw = line, ReceivedAtUtc = DateTime.UtcNow };
                var s = _settings.Load();
                if (!string.IsNullOrWhiteSpace(s.ApiKey) && !string.IsNullOrWhiteSpace(s.MachineId))
                {
                    var ok = await _api.SendDataAsync(s.ApiKey!, s.MachineId!, record);
                    if (!ok) _queue.Enqueue(record);
                }
                else { _queue.Enqueue(record); }
            };
        }
    }
}