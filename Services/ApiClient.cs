using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using SigstreamTelemetryAgent.Models;
using SigstreamTelemetryAgent.Services;

namespace SigstreamTelemetryAgent.Services
{
    // Aligns with server routes:
    //  - POST /api/claim        (body: { api_key, machine_id, description })
    //  - POST /api/heartbeat    (headers: X-Api-Key, X-Device-Id | X-Machine-Id; body: { device_id, heartbeat_time, status })
    //  - POST /data             (headers: X-Api-Key, X-Device-Id; body: { device_id, data, timestamp })
    //  - (optional) /api/status (if present; otherwise returns null)
    public class ApiClient : IApiClient
    {
        private readonly HttpClient _http;
        private readonly ISettingsService? _settings;

        public ApiClient(HttpClient http, ISettingsService? settings = null)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _settings = settings; // optional; used to read MachineId if available
        }

        // -------------------------
        // Registration / Claim
        // -------------------------
        public async Task<RegistrationResult> RegisterAsync(string apiKey, string? deviceLabel)
        {
            try
            {
                var machineId = GetMachineId();

                var payload = new
                {
                    api_key = apiKey,
                    machine_id = machineId,
                    description = deviceLabel // server expects "description" (not "device_label")
                };

                var res = await _http.PostAsJsonAsync("/api/claim", payload).ConfigureAwait(false);
                if (!res.IsSuccessStatusCode)
                {
                    string? msg = null;
                    try
                    {
                        var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                        msg = string.IsNullOrWhiteSpace(json) ? null : json;
                    }
                    catch { /* ignore parse errors */ }

                    return new RegistrationResult
                    {
                        Success = false,
                        MachineId = machineId,
                        Error = msg ?? $"HTTP {(int)res.StatusCode} {res.ReasonPhrase}"
                    };
                }

                // main.py does not return machine_id; treat success as bound/ok.
                return new RegistrationResult
                {
                    Success = true,
                    MachineId = machineId
                };
            }
            catch (Exception ex)
            {
                return new RegistrationResult { Success = false, Error = ex.Message };
            }
        }

        // -------------------------
        // Optional status pre-check
        // -------------------------
        public async Task<StatusResult?> CheckStatusAsync(string apiKey, string machineId)
        {
            try
            {
                var body = new { api_key = apiKey, machine_id = machineId };
                var res = await _http.PostAsJsonAsync("/api/status", body).ConfigureAwait(false);

                if (res.StatusCode == HttpStatusCode.NotFound)
                    return null; // not deployed in prod; treat as "no status info"

                if (!res.IsSuccessStatusCode)
                    return null;

                var sr = await res.Content.ReadFromJsonAsync<StatusResult>().ConfigureAwait(false);
                return sr;
            }
            catch
            {
                return null; // network errors treated as "no status info"
            }
        }

        // -------------------------
        // Heartbeat
        // -------------------------
        public Task<bool> SendHeartbeatAsync(string apiKey, string machineId)
        {
            // Server parses ISO-8601 via datetime.fromisoformat (no trailing 'Z')
            var hbBody = new
            {
                device_id = machineId,
                heartbeat_time = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                status = "online"
            };

            return PostWithHeadersAsync("/api/heartbeat", hbBody, apiKey, machineId);
        }

        // -------------------------
        // Telemetry / Data
        // -------------------------
        public Task<bool> SendDataAsync(string apiKey, string machineId, TelemetryRecord record)
        {
            var epoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            // Server expects: { device_id, data, timestamp }
            // If TelemetryRecord.Raw contains JSON text, it's fine to send as string.
            // (If you later have a parsed object, you can pass that instead.)
            var body = new
            {
                device_id = machineId,
                data = record?.Raw, // keep as-is; server stores/logs it
                timestamp = epoch
            };

            return PostWithHeadersAsync("/data", body, apiKey, machineId);
        }

        // -------------------------
        // Helpers
        // -------------------------
        private async Task<bool> PostWithHeadersAsync(string path, object body, string apiKey, string machineId)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, path)
                {
                    Content = JsonContent.Create(body)
                };

                // main.py accepts either; we send X-Api-Key + X-Device-Id
                req.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
                req.Headers.TryAddWithoutValidation("X-Device-Id", machineId);

                var res = await _http.SendAsync(req).ConfigureAwait(false);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private string GetMachineId()
        {
            try
            {
                var app = _settings?.Load();
                if (!string.IsNullOrWhiteSpace(app?.MachineId))
                    return app!.MachineId!;
            }
            catch { /* ignore and fall back */ }

            // Fallback if settings not available yet
            return Environment.MachineName;
        }
    }
}
