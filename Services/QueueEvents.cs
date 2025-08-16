using System;

namespace SigstreamTelemetryAgent.Services
{
    public sealed class FlushEventArgs : EventArgs
    {
        public int Sent { get; }
        public int Remaining { get; }
        public FlushEventArgs(int sent, int remaining)
        {
            Sent = sent;
            Remaining = remaining;
        }
    }
}
