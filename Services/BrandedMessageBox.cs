using System.Linq;
using System.Windows;
using SigstreamTelemetryAgent.Views.Dialogs;

namespace SigstreamTelemetryAgent.Services
{
    public static class BrandedMessageBox
    {
        public static MessageBoxResult Show(string message)
            => Show(message, "SigStream", MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string message, string caption)
            => Show(message, caption, MessageBoxButton.OK, MessageBoxImage.None);

        public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons)
            => Show(message, caption, buttons, MessageBoxImage.None);

        public static MessageBoxResult Show(string message, string caption, MessageBoxButton buttons, MessageBoxImage icon)
        {
            try
            {
                var owner = ResolveOwner();
                switch (buttons)
                {
                    case MessageBoxButton.OK:
                        {
                            var dlg = new BrandedDialog("SigStream", caption, message, "OK");
                            ShowDialogSafe(dlg, owner);
                            return MessageBoxResult.OK;
                        }
                    case MessageBoxButton.OKCancel:
                        {
                            var dlg = new BrandedDialog("SigStream", caption, message, "OK", "Cancel");
                            return ShowDialogSafe(dlg, owner) == true ? MessageBoxResult.OK : MessageBoxResult.Cancel;
                        }
                    case MessageBoxButton.YesNo:
                        {
                            var dlg = new BrandedDialog("SigStream", caption, message, "Yes", "No");
                            return ShowDialogSafe(dlg, owner) == true ? MessageBoxResult.Yes : MessageBoxResult.No;
                        }
                    default:
                        return System.Windows.MessageBox.Show(message, caption, buttons, icon);
                }
            }
            catch
            {
                return System.Windows.MessageBox.Show(message, caption, buttons, icon);
            }
        }

        private static bool? ShowDialogSafe(BrandedDialog dlg, Window? owner)
        {
            var app = Application.Current;

            if (owner != null)
            {
                dlg.Owner = owner;
                return dlg.ShowDialog();
            }

            if (app != null)
            {
                var original = app.ShutdownMode;
                var changed = false;

                if (original == ShutdownMode.OnLastWindowClose)
                {
                    app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    changed = true;
                }

                try { return dlg.ShowDialog(); }
                finally
                {
                    if (changed)
                        app.ShutdownMode = original;
                }
            }

            return dlg.ShowDialog();
        }

        private static Window? ResolveOwner()
        {
            var app = Application.Current;
            if (app == null || app.Windows == null || app.Windows.Count == 0)
                return null;

            var active = app.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive);
            return active ?? app.Windows[0];
        }
    }
}
