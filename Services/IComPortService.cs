using System;
using System.Collections.Generic;

namespace SigstreamTelemetryAgent.Services
{
    public interface IComPortService
    {
        IEnumerable<string> GetPorts();
        void Open(string portName, int baudRate);
        void Close();
        bool IsOpen { get; }                         // NEW
        event EventHandler<string>? LineReceived;
    }
}
