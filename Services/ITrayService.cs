// Services/ITrayService.cs
namespace SigstreamTelemetryAgent.Services
{
    public interface ITrayService
    {
        void ShowInfoToast(string message);
    }

    public class TrayService : ITrayService
    {
        public void ShowInfoToast(string message) { /* TODO: hook TaskbarIcon */ }
    }
}