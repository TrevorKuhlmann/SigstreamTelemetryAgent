// Views/Pages/AboutPage.xaml.cs
using System.Windows.Controls;

namespace SigstreamTelemetryAgent.Views.Pages
{
    public partial class AboutPage : Page
    {
        public AboutPage(ViewModels.AboutViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}