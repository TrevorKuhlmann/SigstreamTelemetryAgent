using System.Diagnostics;
using System.Windows;

namespace SigstreamTelemetryAgent.Services
{
    public sealed class ToastService : IToastService
    {
        private const string AppTitle = "SigStream Agent";

        // 1-arg overloads just forward to the 2-arg overloads
        public void ShowInfo(string message) => ShowInfo(message, AppTitle);
        public void ShowSuccess(string message) => ShowSuccess(message, "Success");
        public void ShowError(string message) => ShowError(message, "Error");

        public void ShowInfo(string message, string? title) => Show(message, title ?? AppTitle, MessageBoxImage.Information);
        public void ShowSuccess(string message, string? title) => Show(message, title ?? "Success", MessageBoxImage.Information);
        public void ShowError(string message, string? title) => Show(message, title ?? "Error", MessageBoxImage.Error);

        private static void Show(string message, string title, MessageBoxImage icon)
        {
            try { MessageBox.Show(message, title, MessageBoxButton.OK, icon); }
            catch { Debug.WriteLine($"[{title}] {message}"); }
        }
    }
}
