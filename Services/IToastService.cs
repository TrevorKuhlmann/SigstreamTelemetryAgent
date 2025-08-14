using Notifications.Wpf;

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
        private readonly NotificationManager _mgr = new();

        public void ShowInfo(string message) =>
            _mgr.Show(new NotificationContent { Title = "SigStream", Message = message, Type = NotificationType.Information });

        public void ShowSuccess(string message) =>
            _mgr.Show(new NotificationContent { Title = "SigStream", Message = message, Type = NotificationType.Success });

        public void ShowError(string message) =>
            _mgr.Show(new NotificationContent { Title = "SigStream", Message = message, Type = NotificationType.Error });
    }
}
