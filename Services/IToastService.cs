// Services/IToastService.cs
namespace SigstreamTelemetryAgent.Services
{
    public interface IToastService
    {
        void ShowInfo(string message);
        void ShowSuccess(string message);
        void ShowError(string message);
    }

    public class ToastService : IToastService
    {
        public void ShowInfo(string message) { /* TODO: implement */ }
        public void ShowSuccess(string message) { /* TODO */ }
        public void ShowError(string message) { /* TODO */ }
    }
}