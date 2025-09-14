using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using SigstreamTelemetryAgent.Models;
using SigstreamTelemetryAgent.Services;



namespace SigstreamTelemetryAgent.Views.Dialogs
{
    public partial class ReportBugDialog : Window
    {
        private readonly ISettingsService _settings;
        private readonly IApiClient _api;

        public ReportBugDialog()
        {
            InitializeComponent();
            _settings = App.HostInstance.Services.GetRequiredService<ISettingsService>();
            _api      = App.HostInstance.Services.GetRequiredService<IApiClient>();

            // Prefill email if you store it (optional)
            try
            {
                var s = _settings.Load();
                // if you cache user email in settings, fill here. Otherwise leave blank.
                // EmailBox.Text = s.UserEmail ?? "";
            }
            catch { }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            var desc = (DescBox.Text ?? "").Trim();
            if (desc.Length < 10)
            {
                StatusText.Text = "Please describe the issue (min 10 characters).";
                return;
            }

            IsEnabled = false;
            StatusText.Text = "Sending…";

            var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString();
            var osVersion = Environment.OSVersion.VersionString;
            var machine = Environment.MachineName;

            string? logs = null;
            if (IncludeLogs.IsChecked == true)
                logs = ReadTail(LogPath(), 200);

            string? envJson = null;
            if (IncludeEnv.IsChecked == true)
            {
                var envObj = new
                {
                    framework = RuntimeInformation.FrameworkDescription,
                    processArch = RuntimeInformation.ProcessArchitecture.ToString(),
                    osArch = RuntimeInformation.OSArchitecture.ToString(),
                    clr64 = Environment.Is64BitProcess,
                    culture = System.Globalization.CultureInfo.CurrentCulture?.Name
                };
                envJson = JsonSerializer.Serialize(envObj);
            }

            var payload = new BugReportPayload
            {
                Email = (EmailBox.Text ?? "").Trim(),
                Description = desc,
                AppVersion = appVersion,
                OsVersion = osVersion,
                Machine = machine,
                Logs = logs,
                EnvJson = envJson
            };

            try
            {
                var ok = await _api.SendBugReportAsync(payload);
                if (ok)
                {
                    StatusText.Text = "Thanks! Bug report sent.";
                    await Task.Delay(900);
                    DialogResult = true;
                    Close();
                    return;
                }
                StatusText.Text = "Server rejected the request. Opening email…";
                await FallbackMail(payload);
            }
            catch
            {
                StatusText.Text = "Couldn’t reach server. Opening email…";
                await FallbackMail(payload);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        private async Task FallbackMail(BugReportPayload body)
        {
            try
            {
                var json = JsonSerializer.Serialize(body, new JsonSerializerOptions { WriteIndented = false });
                var mailto = $"mailto:admin@sigstreamcloud.com?subject=SigStream%20Bug%20Report&body={Uri.EscapeDataString(json[..Math.Min(1800, json.Length)])}";
                Process.Start(new ProcessStartInfo(mailto) { UseShellExecute = true });
            }
            catch { }
            await Task.Delay(300);
            DialogResult = true;
            Close();
        }



        private static string? LogPath()
        {
            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SigStream", "logs");
            var di = new DirectoryInfo(baseDir);
            if (!di.Exists) return null;
            var latest = di.GetFiles("*.log").OrderByDescending(f => f.LastWriteTimeUtc).FirstOrDefault();
            return latest?.FullName;
        }

        private static string? ReadTail(string? path, int lines)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
                var all = File.ReadAllLines(path);
                var take = Math.Min(lines, all.Length);
                return string.Join(Environment.NewLine, all.Skip(all.Length - take));
            }
            catch { return null; }
        }
    }
}