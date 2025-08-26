namespace SigstreamTelemetryAgent.Services
{
    public interface IToastService
    {
        // 1-arg overloads
        void ShowInfo(string message);
        void ShowSuccess(string message);
        void ShowError(string message);

        // 2-arg overloads
        void ShowInfo(string message, string? title);
        void ShowSuccess(string message, string? title);
        void ShowError(string message, string? title);
    }
}
