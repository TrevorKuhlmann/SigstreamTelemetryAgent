using System;
using System.Drawing;
using System.Windows;
using System.Windows.Forms;

namespace SigstreamTelemetryAgent.Services
{
    public class TrayService : ITrayService, IDisposable
    {
        private NotifyIcon? _icon;
        private Window? _window;
        private bool _reallyExit;

        public void HookWindow(Window window)
        {
            Initialize(window, null, "SigStream Telemetry");
        }

        public void ShowInfoToast(string message)
        {
            try { _icon?.ShowBalloonTip(1200, "SigStream", message ?? string.Empty, ToolTipIcon.Info); } catch { }
        }

        public void Initialize(Window window, Icon? icon = null, string? tooltip = null)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));

            _icon = new NotifyIcon
            {
                Icon = icon ?? SystemIcons.Application,
                Visible = true,
                Text = string.IsNullOrWhiteSpace(tooltip) ? "SigStream" : tooltip
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add(new ToolStripMenuItem("Show", null, (_, __) => ShowWindow()));
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(new ToolStripMenuItem("Exit", null, (_, __) => ExitApp()));
            _icon.ContextMenuStrip = menu;

            _icon.DoubleClick += (_, __) => ShowWindow();

            _window.StateChanged += Window_StateChanged;
            _window.Closing += Window_Closing;
        }

        private void Window_StateChanged(object? sender, EventArgs e)
        {
            if (_window != null && _window.WindowState == WindowState.Minimized)
                HideWindow();
        }

        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_reallyExit) return;
            e.Cancel = true;
            HideWindow();
        }

        private void HideWindow()
        {
            if (_window == null) return;
            _window.Hide();
            ShowInfoToast("Still running in the system tray.");
        }

        private void ShowWindow()
        {
            if (_window == null) return;
            _window.Show();
            if (_window.WindowState == WindowState.Minimized)
                _window.WindowState = WindowState.Normal;
            _window.Activate();
        }

        private void ExitApp()
        {
            _reallyExit = true;
            try { if (_icon != null) { _icon.Visible = false; _icon.Dispose(); } } catch { }
            // Fully qualify to use WPF Application, not WinForms
            System.Windows.Application.Current.Shutdown();
        }

        public void Dispose()
        {
            try { if (_icon != null) { _icon.Visible = false; _icon.Dispose(); } } catch { }
        }
    }
}
