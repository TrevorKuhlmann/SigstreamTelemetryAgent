// Services/ApiClient.cs
using SigstreamTelemetryAgent.Models;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace SigstreamTelemetryAgent.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private readonly ISettingsService _settings;

        public ApiClient(ISettingsService settings)
        {
            _settings = settings;
            _http = new HttpClient { BaseAddress = new Uri(settings.Load().ApiBaseUrl ?? "https://sigstreamcloud.com") };
            _http.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<RegistrationResult> RegisterAsync(string apiKey, string deviceLabel)
        {
            var payload = new { api_key = apiKey, device_label = deviceLabel, machine_id = MachineIdProvider.GetOrCreate() };
            var res = await _http.PostAsJsonAsync("/api/claim", payload);
            if (!res.IsSuccessStatusCode)
                return new RegistrationResult { Success = false, Error = $"HTTP {(int)res.StatusCode}" };

            var json = await res.Content.ReadFromJsonAsync<JsonElement>();
            var machineId = json.GetProperty("machine_id").GetString();
            return new RegistrationResult { Success = true, MachineId = machineId };
        }

        public async Task<StatusResult?> CheckStatusAsync(string apiKey, string machineId)
        {
            var res = await _http.PostAsJsonAsync("/api/status", new { api_key = apiKey, machine_id = machineId });
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<StatusResult>();
        }

        public Task<bool> SendHeartbeatAsync(string apiKey, string machineId)
            => SendBoolAsync("/heartbeat", new { api_key = apiKey, machine_id = machineId });

        public Task<bool> SendDataAsync(string apiKey, string machineId, TelemetryRecord record)
            => SendBoolAsync("/data", new { api_key = apiKey, machine_id = machineId, payload = record });

        private async Task<bool> SendBoolAsync(string path, object payload)
        {
            var res = await _http.PostAsJsonAsync(path, payload);
            return res.IsSuccessStatusCode;
        }
    }

    public static class MachineIdProvider
    {
        private const string FileName = "machine.id";
        public static string GetOrCreate()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "SigStream");
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, FileName);
            if (File.Exists(path)) return File.ReadAllText(path);
            var id = Guid.NewGuid().ToString("N");
            File.WriteAllText(path, id);
            return id;
        }
    }
}