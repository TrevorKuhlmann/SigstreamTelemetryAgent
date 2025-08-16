using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly IToastService _toast;

        private CancellationTokenSource? _reconnectCts;
        private bool _keepConnected;

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

        public ComPortViewModel(IComPortService com,
                                IApiClient api,
                                ISettingsService settings,
                                IOfflineQueue queue,
                                IToastService toast)
        {
            _com = com; _api = api; _settings = settings; _queue = queue; _toast = toast;

            RefreshPortsCommand = new RelayCommand(_ => RefreshPorts());
            ConnectCommand = new RelayCommand(_ => Connect(), _ => !IsConnected && !string.IsNullOrWhiteSpace(SelectedPort));
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsConnected);

            RefreshPorts();

            // Data pipeline
            _com.LineReceived += async (_, line) =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    RecentLines.Insert(0, line.Trim());
                    while (RecentLines.Count > 200) RecentLines.RemoveAt(RecentLines.Count - 1);
                    ReceivedCount++;
                    _ = PulseRx();
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
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        SentCount++;
                        _ = PulseTx();
                    });
                }

                App.Current.Dispatcher.Invoke(() => QueueDepth = _queue.EstimateDepth());
            };

            // Resilience: react to errors/drops
            _com.ConnectionLost += async (_, __) =>
            {
                OnPropertyChanged(nameof(IsConnected));
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
                if (_keepConnected) await StartReconnectLoopAsync();
            };

            _com.Error += (_, ex) =>
            {
                _toast.ShowError($"COM error: {ex.Message}");
            };

            // Auto-connect to last known port on startup
            var st = _settings.Load();
            if (!string.IsNullOrWhiteSpace(st.SelectedComPort))
            {
                SelectedPort = st.SelectedComPort;
                BaudRate = st.BaudRate;
                _keepConnected = true;
                _ = StartReconnectLoopAsync(initialImmediate: true);
            }
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

            try
            {
                _com.Open(SelectedPort!, BaudRate);
                _keepConnected = true;

                // remember last working port/baud
                var s = _settings.Load();
                s.SelectedComPort = SelectedPort;
                s.BaudRate = BaudRate;
                _settings.Save(s);
            }
            catch (Exception ex)
            {
                _toast.ShowError($"Failed to open {SelectedPort}: {ex.Message}");
                _keepConnected = true;
                _ = StartReconnectLoopAsync(initialImmediate: false);
            }
            finally
            {
                OnPropertyChanged(nameof(IsConnected));
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
            }
        }

        private void Disconnect()
        {
            _keepConnected = false;
            _reconnectCts?.Cancel();
            _com.Close();
            OnPropertyChanged(nameof(IsConnected));
            ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
        }

        private async Task StartReconnectLoopAsync(bool initialImmediate = false)
        {
            _reconnectCts?.Cancel();
            _reconnectCts = new CancellationTokenSource();
            var ct = _reconnectCts.Token;

            int attempt = initialImmediate ? 0 : 1;

            while (_keepConnected && !_com.IsOpen && !ct.IsCancellationRequested && !string.IsNullOrWhiteSpace(SelectedPort))
            {
                try
                {
                    _com.Open(SelectedPort!, BaudRate);
                    OnPropertyChanged(nameof(IsConnected));
                    ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
                    _toast.ShowSuccess($"Reconnected {SelectedPort}.");
                    break;
                }
                catch
                {
                    var delay = TimeSpan.FromSeconds(Math.Min(30, Math.Pow(2, attempt))); // 0s/2/4/8/16/30...
                    attempt = Math.Max(1, attempt + 1);
                    try { await Task.Delay(delay, ct); } catch { break; }
                }
            }
        }

        private async Task PulseRx()
        {
            RxPulse = true;
            await Task.Delay(150);
            RxPulse = false;
        }

        private async Task PulseTx()
        {
            TxPulse = true;
            await Task.Delay(150);
            TxPulse = false;
        }
    }
}
