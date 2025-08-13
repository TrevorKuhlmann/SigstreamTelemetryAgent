// Views/Pages/ApiConfigPage.xaml.cs
using System.Windows.Controls;

namespace SigstreamTelemetryAgent.Views.Pages
{
    public partial class ApiConfigPage : Page
    {
        public ApiConfigPage(ViewModels.ApiConfigViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}