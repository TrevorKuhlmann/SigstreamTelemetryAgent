using System.Windows;
using SigstreamTelemetryAgent.Views.Dialogs;

namespace SigstreamTelemetryAgent.Services
{
    public interface IDialogService
    {
        void Info(string message, string header = "Info", Window? owner = null);
        void Error(string message, string header = "Error", Window? owner = null);
        bool Confirm(string message, string header = "Please confirm", Window? owner = null);
    }

    public class DialogService : IDialogService
    {
        private static Window? ResolveOwner(Window? owner)
        {
            if (owner != null) return owner;

            var app = Application.Current;
            if (app != null && app.Windows != null && app.Windows.Count > 0)
                return app.Windows[0];

            return null;
        }

        public void Info(string message, string header = "Info", Window? owner = null)
        {
            var dlg = new BrandedDialog("SigStream", header, message, "OK");
            dlg.Owner = ResolveOwner(owner);
            dlg.ShowDialog();
        }

        public void Error(string message, string header = "Error", Window? owner = null)
        {
            var dlg = new BrandedDialog("SigStream", header, message, "OK");
            dlg.Owner = ResolveOwner(owner);
            dlg.ShowDialog();
        }

        public bool Confirm(string message, string header = "Please confirm", Window? owner = null)
        {
            var dlg = new BrandedDialog("SigStream", header, message, "Yes", "No");
            dlg.Owner = ResolveOwner(owner);
            return dlg.ShowDialog() == true;
        }
    }
}
