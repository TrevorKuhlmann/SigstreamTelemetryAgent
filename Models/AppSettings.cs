using System.Text.Json.Serialization;

namespace SigstreamTelemetryAgent.Models
{
    public class AppSettings
    {
        public string? ApiBaseUrl { get; set; } = "https://sigstreamcloud.com";

        // Runtime value (NOT written to disk)
        [JsonIgnore] public string? ApiKey { get; set; }

        // Encrypted-at-rest value (written to disk)
        public string? ApiKeyProtected { get; set; }

        public string? MachineId { get; set; }
        public string? DeviceLabel { get; set; }
        public string? SelectedComPort { get; set; }
        public int BaudRate { get; set; } = 9600;

        public bool AutoStartOnBoot { get; set; }
        public bool StartMinimized { get; set; } = true;

        public bool SendHeartbeats { get; set; } = true;
        public int HeartbeatSeconds { get; set; } = 30;
    }
}
