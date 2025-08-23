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

        // Existing
        event EventHandler<FlushEventArgs>? Flushed;

        // NEW: live stats
        int Count { get; }          // queued items
        long SizeBytes { get; }     // approx. bytes on disk
        event EventHandler? StatsChanged; // raised on enqueue/dequeue/flush/trim
    }
}
