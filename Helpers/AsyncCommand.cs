// Helpers/AsyncCommand.cs
namespace SigstreamTelemetryAgent.Helpers
{
    public class AsyncCommand : System.Windows.Input.ICommand
    {
        private readonly Func<Task> _run; private bool _busy;
        public event EventHandler? CanExecuteChanged;
        public AsyncCommand(Func<Task> run) { _run = run; }
        public bool CanExecute(object? p) => !_busy;
        public async void Execute(object? p)
        {
            _busy = true; CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            try { await _run(); }
            finally { _busy = false; CanExecuteChanged?.Invoke(this, EventArgs.Empty); }
        }
    }
}