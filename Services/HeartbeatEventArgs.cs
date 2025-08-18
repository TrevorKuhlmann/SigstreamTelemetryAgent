using System;
using System.Net;

namespace SigstreamTelemetryAgent.Services
{
    public sealed class HeartbeatEventArgs : EventArgs
    {
        // Canonical fields
        public DateTime UtcTime { get; }
        public bool Success { get; }
        public HttpStatusCode? StatusCode { get; }

        // Back-compat aliases expected by existing UI code
        public DateTime? Utc => UtcTime;                 // <-- property, nullable to match UI `DateTime?`
        public bool Ok => Success;
        public HttpStatusCode? Code => StatusCode;

        public HeartbeatEventArgs(DateTime utcTime, bool success, HttpStatusCode? statusCode)
        {
            UtcTime = utcTime;
            Success = success;
            StatusCode = statusCode;
        }
    }
}
