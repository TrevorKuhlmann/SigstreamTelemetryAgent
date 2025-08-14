// Services/StartupService.cs
using Microsoft.Win32;
using System.Diagnostics;

namespace SigstreamTelemetryAgent.Services
{
    public class StartupService : IStartupService
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "SigstreamTelemetryAgent";
        private readonly string _exePath;

        public StartupService()
        {
            _exePath = Process.GetCurrentProcess().MainModule!.FileName!;
        }

        public bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
            var val = key?.GetValue(AppName) as string;
            return string.Equals(val, _exePath, StringComparison.OrdinalIgnoreCase);
        }

        public void SetEnabled(bool enabled)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true)
                           ?? Registry.CurrentUser.CreateSubKey(RunKey)!;
            if (enabled) key.SetValue(AppName, _exePath);
            else key.DeleteValue(AppName, false);
        }
    }
}
