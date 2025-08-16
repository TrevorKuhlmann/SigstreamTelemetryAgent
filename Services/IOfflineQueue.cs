using System;
using System.Threading.Tasks;
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services
{
    public interface IOfflineQueue
    {
        void Enqueue(TelemetryRecord record);
        Task FlushAsync(string apiKey, string machineId, Func<TelemetryRecord, Task<bool>> send);
        int EstimateDepth();

        event EventHandler<FlushEventArgs>? Flushed;   // requires QueueEvents.cs
    }
}
