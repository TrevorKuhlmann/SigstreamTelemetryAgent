using System.Windows;
using SigstreamTelemetryAgent.Services;

namespace SigstreamTelemetryAgent.Views
{
    public partial class MainWindow : Window
    {
        // Keep exactly one constructor
        public MainWindow(ViewModels.MainWindowViewModel vm, ITrayService tray)
        {
            InitializeComponent();
            DataContext = vm;
            Loaded += (_, __) =>
            {
                vm.Init(MainFrame);      // MainFrame is the x:Name in XAML
                tray.HookWindow(this);
            };
        }
    }
}
