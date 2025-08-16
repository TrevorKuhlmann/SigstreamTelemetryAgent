using SigstreamTelemetryAgent.Helpers;
using SigstreamTelemetryAgent.Models;
using System.IO;
using System.Text.Json;

namespace SigstreamTelemetryAgent.Services
{
    public class SettingsService : ISettingsService
    {
        private const string Folder = "SigStream";
        private const string FileName = "settings.json";
        public string SettingsPath { get; }

        public SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, Folder);
            Directory.CreateDirectory(dir);
            SettingsPath = Path.Combine(dir, FileName);
        }

        public AppSettings Load()
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var json = File.ReadAllText(SettingsPath);
            var s = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

            // Decrypt into runtime ApiKey if present
            if (!string.IsNullOrWhiteSpace(s.ApiKeyProtected))
            {
                try { s.ApiKey = Crypto.Unprotect(s.ApiKeyProtected); }
                catch { s.ApiKey = null; } // corrupted/foreign user context
            }

            return s;
        }

        public void Save(AppSettings settings)
        {
            // Write encrypted copy; do not serialize ApiKey
            var copy = new AppSettings
            {
                ApiBaseUrl = settings.ApiBaseUrl,
                ApiKeyProtected = string.IsNullOrWhiteSpace(settings.ApiKey) ? null : Crypto.Protect(settings.ApiKey),
                MachineId = settings.MachineId,
                DeviceLabel = settings.DeviceLabel,
                SelectedComPort = settings.SelectedComPort,
                BaudRate = settings.BaudRate,
                AutoStartOnBoot = settings.AutoStartOnBoot,
                StartMinimized = settings.StartMinimized,
                SendHeartbeats = settings.SendHeartbeats,
                HeartbeatSeconds = settings.HeartbeatSeconds
            };

            var json = JsonSerializer.Serialize(copy, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            File.WriteAllText(SettingsPath, json);
        }
    }
}
