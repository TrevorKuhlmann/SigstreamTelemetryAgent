using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;

namespace SigstreamTelemetryAgent.Services
{
    public sealed class ComPortService : IComPortService
    {
        private SerialPort? _port;
        public event EventHandler<string>? LineReceived;

        public bool IsOpen => _port?.IsOpen == true;                 // NEW
        public IEnumerable<string> GetPorts() => SerialPort.GetPortNames().OrderBy(n => n);

        public void Open(string portName, int baudRate)
        {
            Close();
            _port = new SerialPort(portName, baudRate) { NewLine = "\n" };
            _port.DataReceived += OnDataReceived;
            _port.Open();
        }

        private void OnDataReceived(object? sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var line = _port?.ReadLine();
                if (!string.IsNullOrEmpty(line))
                    LineReceived?.Invoke(this, line);
            }
            catch { /* ignore framing/partial reads */ }
        }

        public void Close()
        {
            if (_port == null) return;
            _port.DataReceived -= OnDataReceived;
            if (_port.IsOpen) _port.Close();
            _port.Dispose();
            _port = null;
        }
    }
}
