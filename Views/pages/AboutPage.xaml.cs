using SigstreamTelemetryAgent.ViewModels;
using System.Windows.Controls;

namespace SigstreamTelemetryAgent.Views.Pages
{
    public partial class AboutPage : Page
    {
        public AboutPage(AboutViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
