using System.Net.NetworkInformation;

namespace SleepOnLan.Services
{
    public class NetworkStatusChangedEventArgs : EventArgs
    {
        public NetworkInterface[] Old { get; set; } = Array.Empty<NetworkInterface>();
        public NetworkInterface[] New { get; set; } = Array.Empty<NetworkInterface>();
    }

    public class NetwerkStatusObserver
    {
        public event EventHandler<NetworkStatusChangedEventArgs>? NetworkChanged = null;

        private readonly ILogger<NetwerkStatusObserver> _logger;
        private NetworkInterface[] _oldInterfaces;
        private readonly System.Timers.Timer _timer;

        public NetwerkStatusObserver(ILogger<NetwerkStatusObserver> logger, IConfiguration configuration)
        {
            _logger = logger;

            long networkStatusCheckDelayInMinutes = configuration.GetValue<long>("NetworkStatusObserver:CheckDelayInMinutes", 5);
            _oldInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            _timer = new System.Timers.Timer(networkStatusCheckDelayInMinutes * 60 * 1000);
            _timer.AutoReset = true;
            _timer.Elapsed += (sender, e) => UpdateNetworkStatus();
            _timer.Enabled = configuration.GetValue<bool>("NetworkStatusObserver:Enabled", false);
        }

        private void UpdateNetworkStatus()
        {
            using var ml = new MethodLogger(_logger);

            _timer.Stop();
            var newInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            NetworkStatusChangedEventArgs networkStatusChangedEventArgs = new NetworkStatusChangedEventArgs() { Old = _oldInterfaces, New = newInterfaces };
            bool hasChanges = false;
            if (newInterfaces.Length != _oldInterfaces.Length)
            {
                hasChanges = true;
            }
            if (!hasChanges)
            {
                for (int i = 0; i < _oldInterfaces.Length; i++)
                {
                    if (_oldInterfaces[i].Name != newInterfaces[i].Name || _oldInterfaces[i].OperationalStatus != newInterfaces[i].OperationalStatus)
                    {
                        hasChanges = true;
                        break;
                    }
                }
            }

            _oldInterfaces = newInterfaces;

            if (hasChanges)
            {
                _logger.LogInformation("Network changes detected");
                RaiseNetworkChanged(networkStatusChangedEventArgs);
            }
            _timer.Start();
        }

        private void RaiseNetworkChanged(NetworkStatusChangedEventArgs networkStatusChangedEventArgs)
        {
            if (NetworkChanged != null)
            {
                _logger.LogInformation("Signalling network change to event subscriber(s)");
                NetworkChanged?.Invoke(this, networkStatusChangedEventArgs);
            }
        }
    }
}
