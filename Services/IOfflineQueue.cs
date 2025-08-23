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

        event EventHandler<FlushEventArgs>? Flushed;

        // Live stats
        int Count { get; }           // queued items
        long SizeBytes { get; }       // approx. bytes on disk
        long MaxCapacityBytes { get; } // <-- NEW: hard cap, used for the meter
        event EventHandler? StatsChanged; // enqueue/dequeue/flush/trim


    }






}
