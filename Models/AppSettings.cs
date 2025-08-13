// Models/AppSettings.cs
namespace SigstreamTelemetryAgent.Models
{
    public class AppSettings
    {
        public string? ApiBaseUrl { get; set; } = "https://sigstreamcloud.com";
        public string? ApiKey { get; set; }
        public string? MachineId { get; set; }
        public string? DeviceLabel { get; set; }
        public string? SelectedComPort { get; set; }
        public int BaudRate { get; set; } = 9600;
        public bool SendHeartbeats { get; set; } = true;
        public int HeartbeatSeconds { get; set; } = 30;
    }
}