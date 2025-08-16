// Services/ApiClient.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services
{
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;

        public ApiClient(HttpClient http)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
        }

        public async Task<RegistrationResult> RegisterAsync(string apiKey, string deviceLabel)
        {
            try
            {
                var payload = new { api_key = apiKey, device_label = deviceLabel, machine_id = MachineIdProvider.GetOrCreate() };
                var res = await _http.PostAsJsonAsync("/api/claim", payload);
                if (!res.IsSuccessStatusCode)
                    return new RegistrationResult { Success = false, Error = $"HTTP {(int)res.StatusCode}" };

                var json = await res.Content.ReadFromJsonAsync<JsonElement>();
                var machineId = json.GetProperty("machine_id").GetString();
                return new RegistrationResult { Success = true, MachineId = machineId };
            }
            catch
            {
                return new RegistrationResult { Success = false, Error = "Network error" };
            }
        }

        public async Task<StatusResult?> CheckStatusAsync(string apiKey, string machineId)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("/api/status", new { api_key = apiKey, machine_id = machineId });
                if (!res.IsSuccessStatusCode) return null;
                return await res.Content.ReadFromJsonAsync<StatusResult>();
            }
            catch { return null; }
        }

        public Task<bool> SendHeartbeatAsync(string apiKey, string machineId) =>
            SendBoolAsync("/heartbeat", new { api_key = apiKey, machine_id = machineId });

        public Task<bool> SendDataAsync(string apiKey, string machineId, TelemetryRecord record) =>
            SendBoolAsync("/data", new { api_key = apiKey, machine_id = machineId, payload = record });

        private async Task<bool> SendBoolAsync(string path, object payload)
        {
            try
            {
                var res = await _http.PostAsJsonAsync(path, payload);
                return res.IsSuccessStatusCode;
            }
            catch { return false; }  // after Polly exhausts
        }
    }
}
