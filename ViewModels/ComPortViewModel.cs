using System.Collections.ObjectModel;
using System.Windows.Input;
using SigstreamTelemetryAgent.Models;
using SigstreamTelemetryAgent.Services;
using SigstreamTelemetryAgent.Helpers;

namespace SigstreamTelemetryAgent.ViewModels
{
    public class ComPortViewModel : BaseViewModel
    {
        private readonly IComPortService _com;
        private readonly IApiClient _api;
        private readonly ISettingsService _settings;
        private readonly IOfflineQueue _queue;

        public ObservableCollection<string> Ports { get; } = new();
        public ObservableCollection<string> RecentLines { get; } = new();

        private string? _selectedPort;
        public string? SelectedPort { get => _selectedPort; set { _selectedPort = value; OnPropertyChanged(); } }

        private int _baudRate = 9600;
        public int BaudRate { get => _baudRate; set { _baudRate = value; OnPropertyChanged(); } }

        public bool IsConnected => _com.IsOpen;

        private int _recvCount;
        public int ReceivedCount { get => _recvCount; private set { _recvCount = value; OnPropertyChanged(); } }

        private int _sentCount;
        public int SentCount { get => _sentCount; private set { _sentCount = value; OnPropertyChanged(); } }

        private int _queueDepth;
        public int QueueDepth { get => _queueDepth; private set { _queueDepth = value; OnPropertyChanged(); } }

        private bool _rxPulse;
        public bool RxPulse { get => _rxPulse; private set { _rxPulse = value; OnPropertyChanged(); } }

        private bool _txPulse;
        public bool TxPulse { get => _txPulse; private set { _txPulse = value; OnPropertyChanged(); } }

        public ICommand RefreshPortsCommand { get; }
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }

        public ComPortViewModel(IComPortService com, IApiClient api, ISettingsService settings, IOfflineQueue queue)
        {
            _com = com; _api = api; _settings = settings; _queue = queue;

            RefreshPortsCommand = new RelayCommand(_ => RefreshPorts());
            ConnectCommand = new RelayCommand(_ => Connect(), _ => !IsConnected && !string.IsNullOrWhiteSpace(SelectedPort));
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);

            RefreshPorts();

            _com.LineReceived += async (_, line) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RecentLines.Insert(0, line.Trim());
                    while (RecentLines.Count > 200) RecentLines.RemoveAt(RecentLines.Count - 1);
                    ReceivedCount++;
                    PulseRx();
                });

                var rec = new TelemetryRecord { Raw = line, ReceivedAtUtc = DateTime.UtcNow };
                var s = _settings.Load();

                bool ok = false;
                if (!string.IsNullOrWhiteSpace(s.ApiKey) && !string.IsNullOrWhiteSpace(s.MachineId))
                {
                    ok = await _api.SendDataAsync(s.ApiKey!, s.MachineId!, rec);
                }
                if (!ok)
                {
                    _queue.Enqueue(rec);
                }
                else
                {
                    App.Current.Dispatcher.Invoke(() => { SentCount++; PulseTx(); });
                }

                App.Current.Dispatcher.Invoke(() => QueueDepth = _queue.EstimateDepth());
            };
        }

        private void RefreshPorts()
        {
            Ports.Clear();
            foreach (var p in _com.GetPorts()) Ports.Add(p);
            OnPropertyChanged(nameof(IsConnected));
            QueueDepth = _queue.EstimateDepth();
        }

        private void Connect()
        {
            if (string.IsNullOrWhiteSpace(SelectedPort)) return;
            _com.Open(SelectedPort, BaudRate);
            OnPropertyChanged(nameof(IsConnected));
            ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
        }

        private void Disconnect()
        {
            _com.Close();
            OnPropertyChanged(nameof(IsConnected));
            ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
        }

        private async void PulseRx()
        {
            RxPulse = true;
            await Task.Delay(150);
            RxPulse = false;
        }

        private async void PulseTx()
        {
            TxPulse = true;
            await Task.Delay(150);
            TxPulse = false;
        }
    }
}
