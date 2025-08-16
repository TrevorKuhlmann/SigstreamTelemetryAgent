// Services/IHeartbeatService.cs
using System;
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services
{
    public interface IHeartbeatService
    {
        void Start(AppSettings settings);
        void Stop();

        // events exposed to consumers
        event EventHandler<bool>? RevokedChanged;
        event EventHandler<HeartbeatEventArgs>? Beat;   // <— add this
    }
}
