using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services


{
    // Server contract (main.py):
    //  - POST /api/claim        body: { api_key, machine_id, description }
    //  - POST /api/heartbeat    headers: X-Api-Key, X-Device-Id | X-Machine-Id; body: { device_id, heartbeat_time, status }
    //  - POST /data             headers: X-Api-Key, X-Device-Id; body: { device_id, data, timestamp }
    //  - (optional) POST /api/status (dev only; tolerate 404/5xx)
    public class ApiClient : IApiClient



    {



        public async Task<bool?> ProbeKeyAsync(string apiKey, string machineId)
        {
            // Try /api/status first (if deployed). Returns:
            //   true  => active/ok
            //   false => revoked/unauthorized
            //   null  => network/unknown (don’t flip state)
            try
            {
                var body = new { api_key = apiKey, machine_id = machineId };
                var res = await _http.PostAsJsonAsync("/api/status", body).ConfigureAwait(false);
                LastStatusCode = res.StatusCode;

                if (res.StatusCode == HttpStatusCode.NotFound)
                    throw new HttpRequestException("status route not present");

                if (res.IsSuccessStatusCode)
                {
                    // Accept either { "active": true/false } or { "revoked": true/false }
                    try
                    {
                        using var doc = await JsonDocument.ParseAsync(await res.Content.ReadAsStreamAsync().ConfigureAwait(false)).ConfigureAwait(false);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("revoked", out var rv)) return !rv.GetBoolean();
                        if (root.TryGetProperty("active", out var ac)) return ac.GetBoolean();
                        return true; // if response is 2xx but lacks flags, assume ok
                    }
                    catch { return true; }
                }

                if (res.StatusCode == HttpStatusCode.Forbidden || res.StatusCode == HttpStatusCode.Unauthorized)
                    return false;

                return null;
            }
            catch
            {
                // Fall through to heartbeat-probe
            }

            // Fallback: tiny heartbeat probe (will update last_seen; acceptable for now)
            var probe = new
            {
                device_id = machineId,
                heartbeat_time = DateTime.UtcNow.ToString("yyyy-MM-dd'T'HH:mm:ss"),
                status = "probe"
            };
            var ok = await PostWithHeadersAsync("/api/heartbeat", probe, apiKey, machineId).ConfigureAwait(false);
            var code = LastStatusCode;

            if (!ok && (code == HttpStatusCode.Forbidden || code == HttpStatusCode.Unauthorized))
                return false;

            if (ok) return true;
            return null; // network/unknown
        }






        private readonly HttpClient _http;
        private readonly ISettingsService? _settings;

        public ApiClient(HttpClient http, ISettingsService? settings = null)
        {
            _http = http ?? throw new ArgumentNullException(nameof(http));
            _settings = settings;
        }

        /// <summary>Last HTTP status code from any POST call (used by HeartbeatService to detect revocation).</summary>
        public HttpStatusCode? LastStatusCode { get; private set; }

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
                    description = deviceLabel // server expects "description"
                };

                var res = await _http.PostAsJsonAsync("/api/claim", payload).ConfigureAwait(false);
                LastStatusCode = res.StatusCode;

                if (!res.IsSuccessStatusCode)
                {
                    string? msg = null;
                    try
                    {
                        var json = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                        msg = string.IsNullOrWhiteSpace(json) ? null : json;
                    }
                    catch { /* ignore */ }

                    return new RegistrationResult
                    {
                        Success = false,
                        MachineId = machineId,
                        Error = msg ?? $"HTTP {(int)res.StatusCode} {res.ReasonPhrase}"
                    };
                }

                // main.py does not return machine_id; treat any 2xx as success.
                return new RegistrationResult { Success = true, MachineId = machineId };
            }
            catch (Exception ex)
            {
                LastStatusCode = null;
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
                LastStatusCode = res.StatusCode;

                if (res.StatusCode == HttpStatusCode.NotFound)
                    return null; // not deployed in prod

                if (!res.IsSuccessStatusCode)
                    return null;

                var sr = await res.Content.ReadFromJsonAsync<StatusResult>().ConfigureAwait(false);
                return sr;
            }
            catch
            {
                LastStatusCode = null;
                return null;
            }
        }

        // -------------------------
        // Heartbeat
        // -------------------------
        public Task<bool> SendHeartbeatAsync(string apiKey, string machineId)
        {
            var hbBody = new
            {
                device_id = machineId,
                // server uses datetime.fromisoformat -> omit trailing Z
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

            var body = new
            {
                device_id = machineId,
                data = record?.Raw, // raw payload ok; server stores/logs as-is
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

                // main.py accepts either X-Device-Id or X-Machine-Id; we send X-Device-Id
                req.Headers.TryAddWithoutValidation("X-Api-Key", apiKey);
                req.Headers.TryAddWithoutValidation("X-Device-Id", machineId);

                var res = await _http.SendAsync(req).ConfigureAwait(false);
                LastStatusCode = res.StatusCode;
                return res.IsSuccessStatusCode;
            }
            catch
            {
                LastStatusCode = null;
                return false;
            }
        }




        public async Task<bool> SendBugReportAsync(BugReportPayload payload)
        {
            try
            {
                var res = await _http.PostAsJsonAsync("/api/report-bug", payload)
                                     .ConfigureAwait(false);
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private string GetMachineId() => MachineIdProvider.GetOrCreate(_settings);
    }
}
