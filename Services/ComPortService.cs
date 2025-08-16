using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;

namespace SigstreamTelemetryAgent.Services
{
    public sealed class ComPortService : IComPortService
    {
        private SerialPort? _port;

        public event EventHandler<string>? LineReceived;
        public event EventHandler? ConnectionLost;
        public event EventHandler<Exception>? Error;

        public bool IsOpen => _port?.IsOpen == true;
        public string? CurrentPort => _port?.PortName;

        public IEnumerable<string> GetPorts() => SerialPort.GetPortNames().OrderBy(x => x);

        public void Open(string portName, int baudRate)
        {
            Close();
            try
            {
                _port = new SerialPort(portName, baudRate) { NewLine = "\n", ReadTimeout = 1000 };
                _port.DataReceived += OnDataReceived;
                _port.ErrorReceived += (_, e) => Error?.Invoke(this, new IOException($"Serial error: {e.EventType}"));
                _port.Open();
            }
            catch (Exception ex)
            {
                Close();
                Error?.Invoke(this, ex);
                throw;
            }
        }

        private void OnDataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var line = _port?.ReadLine();
                if (!string.IsNullOrEmpty(line))
                    LineReceived?.Invoke(this, line);
            }
            catch (Exception ex)
            {
                Error?.Invoke(this, ex);
                // treat as a drop (cable pull / device reset)
                try { Close(); } catch { /* ignore */ }
                ConnectionLost?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Close()
        {
            if (_port == null) return;
            try
            {
                _port.DataReceived -= OnDataReceived;
                if (_port.IsOpen) _port.Close();
                _port.Dispose();
            }
            finally { _port = null; }
        }
    }
}
