using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace SigstreamTelemetryAgent.Services
{
    public static class MachineIdProvider
    {
        public static string GetOrCreate(ISettingsService? settings = null)
        {
            try
            {
                var s = settings?.Load();
                if (!string.IsNullOrWhiteSpace(s?.MachineId))
                    return s!.MachineId!;
            }
            catch { /* ignore */ }

            var id = ComputeStableId();

            try
            {
                if (settings != null)
                {
                    var s = settings.Load() ?? new Models.AppSettings();
                    s.MachineId = id;
                    settings.Save(s);
                }
            }
            catch { /* ignore */ }

            return id;
        }

        private static string ComputeStableId()
        {
            var parts = new List<string>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
                    var machineGuid = key?.GetValue("MachineGuid") as string;
                    if (!string.IsNullOrWhiteSpace(machineGuid))
                        parts.Add(machineGuid!);
                }
                catch { }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                try
                {
                    var mi = File.ReadAllText("/etc/machine-id").Trim();
                    if (!string.IsNullOrWhiteSpace(mi)) parts.Add(mi);
                }
                catch { }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    var dump = ExecAndRead("ioreg", "-rd1 -c IOPlatformExpertDevice") ?? "";
                    var uuid = dump.Split('\n').FirstOrDefault(l => l.Contains("IOPlatformUUID"))
                                   ?.Split('"').Reverse().Skip(1).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(uuid)) parts.Add(uuid!);
                }
                catch { }
            }

            try
            {
                var macs = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                n.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                                n.OperationalStatus == OperationalStatus.Up)
                    .Select(n => n.GetPhysicalAddress()?.ToString())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct();
                parts.AddRange(macs!);
            }
            catch { }

            if (parts.Count == 0)
                parts.Add(Environment.MachineName);

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("|", parts.Distinct())));
            return Convert.ToHexString(hash); // 64-char upper hex
        }

        private static string? ExecAndRead(string fileName, string args)
        {
            try
            {
                var p = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = fileName,
                        Arguments = args,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                var s = p.StandardOutput.ReadToEnd();
                p.WaitForExit(2000);
                return s;
            }
            catch { return null; }
        }
    }
}
