using System;
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services
{
    public interface ISettingsService
    {
        AppSettings Load();
        void Save(AppSettings settings);
        string SettingsPath { get; }

        // 🔔 Raised whenever settings are saved/changed
        event EventHandler? SettingsChanged;
    }
}
