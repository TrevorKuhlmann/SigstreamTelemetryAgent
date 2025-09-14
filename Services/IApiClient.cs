// Services/IApiClient.cs
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services
{
    public class RegistrationResult { public bool Success { get; set; } public string? MachineId { get; set; } public string? Error { get; set; } }
    public class StatusResult { public bool IsRevoked { get; set; } public string? SubscriptionStatus { get; set; } }

    public interface IApiClient
    {
        Task<RegistrationResult> RegisterAsync(string apiKey, string deviceLabel);
        Task<StatusResult?> CheckStatusAsync(string apiKey, string machineId);
        Task<bool> SendHeartbeatAsync(string apiKey, string machineId);
        Task<bool> SendDataAsync(string apiKey, string machineId, TelemetryRecord record);


        Task<bool> SendBugReportAsync(BugReportPayload payload);
    }
}