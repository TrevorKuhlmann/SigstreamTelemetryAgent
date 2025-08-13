// ViewModels/BaseViewModel.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SigstreamTelemetryAgent.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}