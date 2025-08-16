using SigstreamTelemetryAgent.Models;
using System.IO;
using System.Text.Json;

namespace SigstreamTelemetryAgent.Services
{
    public class OfflineQueue : IOfflineQueue
    {
        private readonly string _path;
        public event EventHandler<FlushEventArgs>? Flushed;

        // caps
        private const long MaxBytes = 20L * 1024 * 1024;  // 20 MB hard cap
        private const long TrimToBytes = 15L * 1024 * 1024; // trim target

        public OfflineQueue()
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SigStream");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "offline-queue.jsonl");
        }

        public void Enqueue(TelemetryRecord rec)
        {
            TrimIfOversize(); // keep size in check before appending
            File.AppendAllText(_path, JsonSerializer.Serialize(rec) + Environment.NewLine);
        }

        public async Task FlushAsync(string apiKey, string machineId, Func<TelemetryRecord, Task<bool>> send)
        {
            if (!File.Exists(_path)) { Flushed?.Invoke(this, new FlushEventArgs(0, 0)); return; }

            var lines = await File.ReadAllLinesAsync(_path);
            var keep = new List<string>(capacity: lines.Length);
            int sent = 0;

            foreach (var line in lines)
            {
                var rec = JsonSerializer.Deserialize<TelemetryRecord>(line);
                if (rec == null) continue;
                var ok = await send(rec);
                if (ok) sent++; else keep.Add(line);
            }

            await File.WriteAllLinesAsync(_path, keep);
            TrimIfOversize();
            Flushed?.Invoke(this, new FlushEventArgs(sent, keep.Count));
        }

        public int EstimateDepth()
        {
            if (!File.Exists(_path)) return 0;
            var count = 0;
            using var sr = File.OpenText(_path);
            while (sr.ReadLine() is not null) count++;
            return count;
        }

        private void TrimIfOversize()
        {
            try
            {
                var fi = new FileInfo(_path);
                if (!fi.Exists || fi.Length <= MaxBytes) return;

                // keep the newest lines until ~TrimToBytes
                var all = File.ReadAllLines(_path);
                long bytes = 0;
                var keep = new Stack<string>();
                for (int i = all.Length - 1; i >= 0; i--)
                {
                    var line = all[i];
                    bytes += (line.Length + Environment.NewLine.Length);
                    keep.Push(line);
                    if (bytes >= TrimToBytes) break;
                }
                File.WriteAllLines(_path, keep.ToArray());
            }
            catch { /* best-effort trimming */ }
        }
    }
}
