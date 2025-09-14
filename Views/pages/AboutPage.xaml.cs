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



        private void ReportBug_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var dlg = new SigstreamTelemetryAgent.Views.Dialogs.ReportBugDialog();
            dlg.Owner = System.Windows.Window.GetWindow(this);
            dlg.ShowDialog();
        }

    }
}
