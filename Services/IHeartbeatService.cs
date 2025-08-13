// Services/IHeartbeatService.cs
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services
{
    public interface IHeartbeatService
    {
        void Start(AppSettings settings);
        void Stop();
        event EventHandler<bool>? RevokedChanged;
    }
}