using System;
using System.Collections.Generic;

namespace SigstreamTelemetryAgent.Services
{
    public interface IComPortService
    {
        IEnumerable<string> GetPorts();
        void Open(string portName, int baudRate);
        void Close();

        bool IsOpen { get; }
        string? CurrentPort { get; }

        event EventHandler<string>? LineReceived;
        event EventHandler? ConnectionLost;           // raised on unexpected drop
        event EventHandler<Exception>? Error;         // raised on serial errors
    }
}
