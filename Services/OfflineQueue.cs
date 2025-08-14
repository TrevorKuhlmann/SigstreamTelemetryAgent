// Services/OfflineQueue.cs fdfgfdgdfg
using SigstreamTelemetryAgent.Models;
using System.IO;
using System.Text.Json;

namespace SigstreamTelemetryAgent.Services
{
    public class OfflineQueue : IOfflineQueue
    {
        private readonly string _path;

        public OfflineQueue()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SigStream");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "offline-queue.jsonl");
        }

        public void Enqueue(TelemetryRecord rec)
        {
            File.AppendAllText(_path, JsonSerializer.Serialize(rec) + "\n");
        }

        public async Task FlushAsync(string apiKey, string machineId, Func<TelemetryRecord, Task<bool>> send)
        {
            if (!File.Exists(_path)) return;
            var lines = await File.ReadAllLinesAsync(_path);
            var keep = new List<string>();
            foreach (var line in lines)
            {
                var rec = JsonSerializer.Deserialize<TelemetryRecord>(line);
                if (rec == null) continue;
                var ok = await send(rec);
                if (!ok) keep.Add(line);
            }
            await File.WriteAllLinesAsync(_path, keep);
        }
    }
}