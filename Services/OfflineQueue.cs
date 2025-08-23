using SigstreamTelemetryAgent.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace SigstreamTelemetryAgent.Services
{
    public class OfflineQueue : IOfflineQueue
    {
        private readonly string _path;
        private readonly SemaphoreSlim _io = new(1, 1);

        public event EventHandler<FlushEventArgs>? Flushed;
        public event EventHandler? StatsChanged;

        private const long MaxBytes = 20L * 1024 * 1024;  // 20 MB cap
        private const long TrimToBytes = 15L * 1024 * 1024;

        // live stats
        private int _count;
        private long _sizeBytes;

        public int Count => _count;
        public long SizeBytes => Interlocked.Read(ref _sizeBytes);
        public long MaxCapacityBytes => MaxBytes;


        public OfflineQueue()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "SigStream");
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, "offline-queue.jsonl");

            try
            {
                if (File.Exists(_path))
                {
                    _sizeBytes = new FileInfo(_path).Length;
                    int lines = 0;
                    using var sr = File.OpenText(_path);
                    while (sr.ReadLine() is not null) lines++;
                    _count = lines;
                }
            }
            catch { _count = 0; _sizeBytes = 0; }
        }

        public void Enqueue(TelemetryRecord rec)
        {
            var line = JsonSerializer.Serialize(rec) + Environment.NewLine;
            var bytes = Encoding.UTF8.GetByteCount(line);

            _io.Wait();
            try
            {
                TrimIfOversize_NoLock();
                File.AppendAllText(_path, line);
                _count++;
                _sizeBytes += bytes;
            }
            finally { _io.Release(); }

            StatsChanged?.Invoke(this, EventArgs.Empty);
        }

        public async System.Threading.Tasks.Task FlushAsync(
            string apiKey,
            string machineId,
            Func<TelemetryRecord, System.Threading.Tasks.Task<bool>> send)
        {
            string[] lines;

            // Read snapshot under lock
            await _io.WaitAsync();
            try
            {
                if (!File.Exists(_path))
                {
                    Flushed?.Invoke(this, new FlushEventArgs(0, 0));
                    return;
                }
                lines = File.ReadAllLines(_path);
            }
            finally { _io.Release(); }

            // Send outside the lock
            var keep = new List<string>(lines.Length);
            int sent = 0;
            foreach (var line in lines)
            {
                var rec = JsonSerializer.Deserialize<TelemetryRecord>(line);
                if (rec == null) continue;
                if (await send(rec)) sent++; else keep.Add(line);
            }

            // Write remaining under lock
            await _io.WaitAsync();
            try
            {
                File.WriteAllLines(_path, keep);
                _count = keep.Count;
                _sizeBytes = File.Exists(_path) ? new FileInfo(_path).Length : 0;
                TrimIfOversize_NoLock();
            }
            finally { _io.Release(); }

            Flushed?.Invoke(this, new FlushEventArgs(sent, _count));
            StatsChanged?.Invoke(this, EventArgs.Empty);
        }

        public int EstimateDepth() => _count;   // 🔄 no file open

        // expects _io held
        private void TrimIfOversize_NoLock()
        {
            try
            {
                if (!File.Exists(_path)) return;

                var fi = new FileInfo(_path);
                if (fi.Length <= MaxBytes) return;

                long bytes = 0;
                var all = File.ReadAllLines(_path);
                var keep = new Stack<string>();
                for (int i = all.Length - 1; i >= 0; i--)
                {
                    var line = all[i];
                    bytes += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
                    keep.Push(line);
                    if (bytes >= TrimToBytes) break;
                }

                var arr = keep.ToArray();
                File.WriteAllLines(_path, arr);

                _count = arr.Length;
                _sizeBytes = new FileInfo(_path).Length;
            }
            catch { /* best effort */ }
        }
    }
}
