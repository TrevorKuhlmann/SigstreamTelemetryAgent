// Helpers/RelayCommand.cs
namespace SigstreamTelemetryAgent.Helpers
{
    public class RelayCommand : System.Windows.Input.ICommand
    {
        private readonly Action<object?> _execute; private readonly Func<object?, bool>? _can;
        public event EventHandler? CanExecuteChanged;
        public RelayCommand(Action<object?> exec, Func<object?, bool>? can = null) { _execute = exec; _can = can; }
        public bool CanExecute(object? p) => _can?.Invoke(p) ?? true;
        public void Execute(object? p) => _execute(p);
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}