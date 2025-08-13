// Models/TelemetryRecord.cs
namespace SigstreamTelemetryAgent.Models
{
    public class TelemetryRecord
    {
        public string Raw { get; set; } = string.Empty;
        public DateTime ReceivedAtUtc { get; set; } = DateTime.UtcNow;
    }
}