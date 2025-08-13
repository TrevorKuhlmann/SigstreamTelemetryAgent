// Views/Pages/ComPortPage.xaml.cs
using System.Windows.Controls;

namespace SigstreamTelemetryAgent.Views.Pages
{
    public partial class ComPortPage : Page
    {
        public ComPortPage(ViewModels.ComPortViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}