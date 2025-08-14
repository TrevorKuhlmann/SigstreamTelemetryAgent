using Hardcodet.Wpf.TaskbarNotification;
using System.Windows;

namespace SigstreamTelemetryAgent.Services
{
    public class TrayService : ITrayService
    {
        private TaskbarIcon? _tray;

        private TaskbarIcon Tray => _tray ??=
            (TaskbarIcon)Application.Current.FindResource("TrayIcon");

        public void ShowInfoToast(string message) =>
            Tray.ShowBalloonTip("SigStream", message, BalloonIcon.Info);

        public void HookWindow(Window window)
        {
            Tray.TrayMouseDoubleClick += (_, __) =>
            {
                window.ShowInTaskbar = true;
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                    window.WindowState = WindowState.Normal;
                window.Activate();
            };
        }
    }
}
