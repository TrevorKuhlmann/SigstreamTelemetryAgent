using System.Windows;

namespace SigstreamTelemetryAgent.Views.Dialogs
{
    public partial class BrandedDialog : Window
    {
        public BrandedDialog(string title, string header, string message,
                             string primaryText = "OK",
                             string? secondaryText = null)
        {
            InitializeComponent();
            DataContext = new VM(title, header, message, primaryText, secondaryText);
        }

        public class VM
        {
            public string Title { get; }
            public string Header { get; }
            public string Message { get; }
            public string PrimaryText { get; }
            public string? SecondaryText { get; }
            public bool HasSecondary => !string.IsNullOrWhiteSpace(SecondaryText);
            public VM(string title, string header, string message, string primary, string? secondary)
            { Title = title; Header = header; Message = message; PrimaryText = primary; SecondaryText = secondary; }
        }

        private void Primary_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Secondary_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
