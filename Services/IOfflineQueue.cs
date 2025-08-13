// Services/IOfflineQueue.cs
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services
{
    public interface IOfflineQueue
    {
        void Enqueue(TelemetryRecord record);
        Task FlushAsync(string apiKey, string machineId, Func<TelemetryRecord, Task<bool>> send);
    }
}