using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SigstreamTelemetryAgent.Services
{
    public class QueueFlushService : BackgroundService
    {
        private readonly IOfflineQueue _queue;
        private readonly ISettingsService _settings;
        private readonly IApiClient _api;
        private readonly ILogger<QueueFlushService> _log;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public QueueFlushService(IOfflineQueue queue, ISettingsService settings, IApiClient api, ILogger<QueueFlushService> log)
        {
            _queue = queue; _settings = settings; _api = api; _log = log;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Gentle initial delay so app can finish booting
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await FlushOnceAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken); // run every 60s
            }
        }

        private async Task FlushOnceAsync(CancellationToken ct)
        {
            if (!await _gate.WaitAsync(0, ct)) return; // avoid overlap
            try
            {
                var s = _settings.Load();
                if (string.IsNullOrWhiteSpace(s.ApiKey) || string.IsNullOrWhiteSpace(s.MachineId))
                    return; // not registered yet

                await _queue.FlushAsync(s.ApiKey!, s.MachineId!, rec => _api.SendDataAsync(s.ApiKey!, s.MachineId!, rec));
            }
            catch (Exception ex)
            {
                _log.LogDebug(ex, "Queue flush skipped (probably offline/circuit open)");
            }
            finally
            {
                _gate.Release();
            }
        }
    }
}
