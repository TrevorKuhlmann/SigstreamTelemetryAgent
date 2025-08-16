using System.Windows.Controls;

namespace SigstreamTelemetryAgent.Views.Pages
{
    public partial class DashboardPage : Page
    {
        public DashboardPage(ViewModels.DashboardViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
