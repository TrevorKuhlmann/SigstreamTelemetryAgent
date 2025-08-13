// Services/ISettingsService.cs
using SigstreamTelemetryAgent.Models;

namespace SigstreamTelemetryAgent.Services
{
    public interface ISettingsService
    {
        AppSettings Load();
        void Save(AppSettings settings);
        string SettingsPath { get; }
    }
}