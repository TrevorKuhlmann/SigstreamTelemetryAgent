// Services/SettingsService.cs
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
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
    }
}