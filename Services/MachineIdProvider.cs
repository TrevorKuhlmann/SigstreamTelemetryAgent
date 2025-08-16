using System;
using System.IO;

namespace SigstreamTelemetryAgent.Services
{
    public static class MachineIdProvider
    {
        private const string FileName = "machine.id";

        public static string GetOrCreate()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var dir = Path.Combine(appData, "SigStream");
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, FileName);
            if (File.Exists(path)) return File.ReadAllText(path);

            var id = Guid.NewGuid().ToString("N");
            File.WriteAllText(path, id);
            return id;
        }
    }
}
