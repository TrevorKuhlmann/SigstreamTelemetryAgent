namespace SigstreamTelemetryAgent.Services
{
    public interface ITrayService
    {
        void ShowInfoToast(string message);
        void HookWindow(System.Windows.Window window); // if you call this
    }
}
