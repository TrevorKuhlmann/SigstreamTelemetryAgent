// Views/MainWindow.xaml.cs
using System.Windows;

namespace SigstreamTelemetryAgent.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(ViewModels.MainWindowViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
            Loaded += (_, __) => vm.Init(MainFrame);
        }
    }
}