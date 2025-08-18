using SigstreamTelemetryAgent.Helpers;
using SigstreamTelemetryAgent.Models;
using System;
using System.IO;
using System.Text.Json;

namespace SigstreamTelemetryAgent.Services
{
    public class SettingsService : ISettingsService
    {
        private const string Folder = "SigStream";
        private const string FileName = "settings.json";
        public string SettingsPath { get; }

        private AppSettings? _cache;
        public event EventHandler? SettingsChanged; // 🔔 notify when settings saved

        public SettingsService()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, Folder);
            Directory.CreateDirectory(dir);
            SettingsPath = Path.Combine(dir, FileName);
        }

        public AppSettings Load()
        {
            if (_cache != null) return _cache; // always return same instance

            if (!File.Exists(SettingsPath))
            {
                _cache = new AppSettings();
                return _cache;
            }

            var json = File.ReadAllText(SettingsPath);
            var s = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();

            if (!string.IsNullOrWhiteSpace(s.ApiKeyProtected))
            {
                try { s.ApiKey = Crypto.Unprotect(s.ApiKeyProtected); }
                catch { s.ApiKey = null; }
            }

            _cache = s;
            return _cache;
        }

        public void Save(AppSettings settings)
        {
            // Ensure _cache exists and remains the single shared instance
            if (_cache is null) _cache = settings;
            else if (!ReferenceEquals(_cache, settings))
            {
                // copy mutated values into the shared object
                _cache.ApiBaseUrl = settings.ApiBaseUrl;
                _cache.ApiKey = settings.ApiKey;
                _cache.ApiKeyProtected = settings.ApiKeyProtected;
                _cache.MachineId = settings.MachineId;
                _cache.DeviceLabel = settings.DeviceLabel;
                _cache.SelectedComPort = settings.SelectedComPort;
                _cache.BaudRate = settings.BaudRate;
                _cache.AutoStartOnBoot = settings.AutoStartOnBoot;
                _cache.StartMinimized = settings.StartMinimized;
                _cache.SendHeartbeats = settings.SendHeartbeats;
                _cache.HeartbeatSeconds = settings.HeartbeatSeconds;
            }

            var copy = new AppSettings
            {
                ApiBaseUrl = _cache.ApiBaseUrl,
                ApiKeyProtected = string.IsNullOrWhiteSpace(_cache.ApiKey) ? null : Crypto.Protect(_cache.ApiKey),
                MachineId = _cache.MachineId,
                DeviceLabel = _cache.DeviceLabel,
                SelectedComPort = _cache.SelectedComPort,
                BaudRate = _cache.BaudRate,
                AutoStartOnBoot = _cache.AutoStartOnBoot,
                StartMinimized = _cache.StartMinimized,
                SendHeartbeats = _cache.SendHeartbeats,
                HeartbeatSeconds = _cache.HeartbeatSeconds
            };

            var json = JsonSerializer.Serialize(copy, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
            File.WriteAllText(SettingsPath, json);

            // 🔔 Broadcast change so MainWindowViewModel picks it up
            SettingsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
