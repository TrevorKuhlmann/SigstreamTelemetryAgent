namespace SigstreamTelemetryAgent.Models
{
    public sealed class BugReportPayload
    {
        public string? Email { get; set; }
        public string Description { get; set; } = "";
        public string? AppVersion { get; set; }
        public string? OsVersion { get; set; }
        public string? Machine { get; set; }
        public string? Logs { get; set; }
        public string? EnvJson { get; set; }
    }
}
