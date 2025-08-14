// Services/IStartupService.cs
namespace SigstreamTelemetryAgent.Services
{
    public interface IStartupService
    {
        bool IsEnabled();
        void SetEnabled(bool enabled);
    }
}
