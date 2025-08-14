using System;
using System.Windows;
using System.Windows.Controls;
using SigstreamTelemetryAgent.ViewModels;   // MainWindowViewModel
using SigstreamTelemetryAgent.Services;     // ITrayService

namespace SigstreamTelemetryAgent.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel vm, ITrayService tray)
        {
            InitializeComponent();
            DataContext = vm;

            Loaded += (_, __) =>
            {
                vm.Init(MainFrame);
                tray.HookWindow(this);
            };
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Minimized) Hide();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            // e.Cancel = true; Hide(); // enable if you want close-to-tray
            base.OnClosing(e);
        }
    }
}
