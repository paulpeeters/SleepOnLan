using SleepOnLan.Services;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SleepOnLan
{
    public class UdpService : BackgroundService
    {
        private readonly ILogger<UdpService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SleepService _sleepService;
        private readonly NetwerkStatusObserver _netwerkStatusObserver;
        private readonly object _macAdressesLock = new();
        private HashSet<string> _macAddresses = new();

        public UdpService(ILogger<UdpService> logger, IConfiguration configuration, SleepService sleepService, NetwerkStatusObserver netwerkStatusObserver)
        {
            using var ml = new MethodLogger(logger);

            _logger = logger;
            _configuration = configuration;
            _sleepService = sleepService;
            _netwerkStatusObserver = netwerkStatusObserver;
        }

        private void InitializeMacAddresses()
        {
            using var ml = new MethodLogger(_logger);

            try
            {
                _logger.LogInformation("Initializing MAC addresses to listen to");
                HashSet<string> _newMacAddresses = new();
                _configuration.GetSection("UdpService:MacAddresses").Get<string[]>()?.ToList().ForEach(item =>
                {
                    var macAddress = MacAddressHelpers.StringToMacAddress(item);
                    if (macAddress is not null)
                    {
                        var s = MacAddressHelpers.MacAddressToString(macAddress);
                        _logger.LogInformation("Adding MAC Address {MacAddress} from config", s);
                        _newMacAddresses.Add(s);
                    }
                });

                NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up && (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet || nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    .Select(nic => nic)
                    .ToList()
                    .ForEach(nic =>
                    {
                        var item = nic.GetPhysicalAddress().GetAddressBytes();
                        if (item.Length == 6)
                        {
                            var s = MacAddressHelpers.MacAddressToString(MacAddressHelpers.ReverseMacAddress(item));
                            _logger.LogInformation("Adding MAC Address {MacAddress} from network interfaces", s);
                            _newMacAddresses.Add(s);
                        }
                    });
                lock (_macAdressesLock)
                {
                    _macAddresses = _newMacAddresses;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var ml = new MethodLogger(_logger);

            InitializeMacAddresses();

            long throttleTime = _configuration.GetValue<long>("UdpService:ThrottleTimeInSeconds", 5);

            System.Timers.Timer throttleTimer = new(throttleTime * 1000);
            throttleTimer.AutoReset = false;
            throttleTimer.Elapsed += async (sender, e) => await _sleepService.ExecuteSleepCommand(stoppingToken);
            throttleTimer.Enabled = false;

            int udpPort = _configuration.GetValue<int>("UdpService:Port", 9);
            using UdpClient listener = new UdpClient(udpPort);
            _logger.LogInformation("Started listining on UDP port {UdpPort}", udpPort);

            _netwerkStatusObserver.NetworkChanged += (sender, e) => InitializeMacAddresses();

            stoppingToken.ThrowIfCancellationRequested();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await listener.ReceiveAsync(stoppingToken);
                    _logger.LogInformation("Received UDP packet from {UdpFrom} with length {UdpMessageLength}", result.RemoteEndPoint, result.Buffer.Length);
                    if (!(result.Buffer.Length != 102 || result.Buffer.Take(6).Any(x => x != 0xff)))
                    {
                        var macAddress = MacAddressHelpers.MacAddressToString(result.Buffer.Skip(6).Take(6).ToArray());
                        bool startSleep = false;
                        lock (_macAdressesLock)
                        {
                            startSleep = _macAddresses.Contains(macAddress);
                        }
                        if (startSleep)
                        {
                            _logger.LogInformation("Accepted MAC address {MacAddress}", macAddress);
                            throttleTimer.Stop();
                            throttleTimer.Start();
                        }
                        else
                        {
                            _logger.LogInformation("Ignored MAC address {MacAddress}", macAddress);
                        }
                    }
                    else
                    {
                        _logger.LogInformation("Ignored non magic WOL packet");
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Stopped listining on UDP");
                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception occured, delaying 5 seconds");
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
            listener.Close();
        }
    }
}
