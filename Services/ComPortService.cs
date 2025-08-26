using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;

namespace SigstreamTelemetryAgent.Services
{
    public enum ComOpenFailureReason
    {
        None = 0,
        NoPortsPresent,
        NoPortSelected,
        PortNotFound,
        PortBusy,         // another app has the port open
        IoError,
        Unknown
    }

    public sealed class ComOpenException : Exception
    {
        public ComOpenFailureReason Reason { get; }
        public string? PortName { get; }

        public ComOpenException(ComOpenFailureReason reason, string? portName, string? message = null, Exception? inner = null)
            : base(message ?? reason.ToString(), inner)
        {
            Reason = reason;
            PortName = portName;
        }
    }

    public readonly record struct ComOpenResult(
        bool Success,
        ComOpenFailureReason Reason,
        string? Message = null
    )
    {
        public static ComOpenResult Ok() => new(true, ComOpenFailureReason.None, null);
        public static ComOpenResult Fail(ComOpenFailureReason r, string? msg = null) => new(false, r, msg);
    }

    public sealed class ComPortService : IComPortService
    {
        private SerialPort? _port;

        public event EventHandler<string>? LineReceived;
        public event EventHandler? ConnectionLost;
        public event EventHandler<Exception>? Error;

        public bool IsOpen => _port?.IsOpen == true;
        public string? CurrentPort => _port?.PortName;

        public IEnumerable<string> GetPorts() => SerialPort.GetPortNames().OrderBy(x => x);

        /// <summary>
        /// Non-throwing open that returns a structured result you can use to decide what to show.
        /// </summary>
        public ComOpenResult TryOpen(string? portName, int baudRate)
        {
            Close();

            var ports = SerialPort.GetPortNames();
            if (ports.Length == 0)
                return ComOpenResult.Fail(ComOpenFailureReason.NoPortsPresent, "No serial (COM) ports detected.");

            if (string.IsNullOrWhiteSpace(portName))
                return ComOpenResult.Fail(ComOpenFailureReason.NoPortSelected, "No COM port selected.");

            if (!ports.Contains(portName, StringComparer.OrdinalIgnoreCase))
                return ComOpenResult.Fail(ComOpenFailureReason.PortNotFound, $"Configured port \"{portName}\" is not available.");

            try
            {
                _port = new SerialPort(portName, baudRate)
                {
                    NewLine = "\n",
                    ReadTimeout = 1000,
                    WriteTimeout = 1000,
                    DtrEnable = true,
                    RtsEnable = true
                };
                _port.DataReceived += OnDataReceived;
                _port.ErrorReceived += (_, e) => Error?.Invoke(this, new IOException($"Serial error: {e.EventType}"));
                _port.Open();
                return ComOpenResult.Ok();
            }
            catch (UnauthorizedAccessException ex)
            {
                Close();
                Error?.Invoke(this, ex);
                return ComOpenResult.Fail(ComOpenFailureReason.PortBusy, $"Port \"{portName}\" is in use by another application.");
            }
            catch (IOException ex)
            {
                Close();
                Error?.Invoke(this, ex);
                return ComOpenResult.Fail(ComOpenFailureReason.IoError, $"I/O error on \"{portName}\": {ex.Message}");
            }
            catch (Exception ex)
            {
                Close();
                Error?.Invoke(this, ex);
                return ComOpenResult.Fail(ComOpenFailureReason.Unknown, ex.Message);
            }
        }

        /// <summary>
        /// Throwing version kept for existing callers.
        /// </summary>
        public void Open(string portName, int baudRate)
        {
            var res = TryOpen(portName, baudRate);
            if (!res.Success)
                throw new ComOpenException(res.Reason, portName, res.Message);
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
