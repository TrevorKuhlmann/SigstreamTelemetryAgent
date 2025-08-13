using System;
using System.Collections.Generic;

namespace SigstreamTelemetryAgent.Services
{
    public interface IComPortService
    {
        IEnumerable<string> GetPorts();
        void Open(string portName, int baudRate);
        void Close();
        event EventHandler<string>? LineReceived;
    }
}
