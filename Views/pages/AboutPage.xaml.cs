using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using SigstreamTelemetryAgent.ViewModels;

namespace SigstreamTelemetryAgent.Views.Pages
{
    public partial class AboutPage : Page
    {
        public AboutPage(AboutViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }

        private void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch
            {
                // ignore
            }
            e.Handled = true;
        }
    }
}
