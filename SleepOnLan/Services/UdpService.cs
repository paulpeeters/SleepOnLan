using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SleepOnLan
{
    public class UdpService : BackgroundService
    {
        private readonly ILogger<UdpService> _logger;
        private readonly IConfiguration _configuration;
        private readonly SleepService _sleepService;
        HashSet<string> _macAddresses = new();

        public UdpService(ILogger<UdpService> logger, IConfiguration configuration, SleepService sleepService)
        {
            using var ml = new MethodLogger(logger);

            _logger = logger;
            _configuration = configuration;
            _sleepService = sleepService;
        }

        private void InitializeMacAddresses(object? _ = null)
        {
            using var ml = new MethodLogger(_logger);

            try
            {
                _logger.LogInformation("Initializing MAC addresses to listen to");
                _macAddresses.Clear();
                _configuration.GetSection("UdpService:MacAddresses").Get<string[]>()?.ToList().ForEach(item =>
                {
                    var macAddress = MacAddressHelpers.StringToMacAddress(item);
                    if (macAddress is not null)
                    {
                        var s = MacAddressHelpers.MacAddressToString(macAddress);
                        _logger.LogInformation("Adding MAC Address {MacAddress} from config", s);
                        _macAddresses.Add(s);
                    }
                });

                NetworkInterface
                    .GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up && (nic.NetworkInterfaceType == NetworkInterfaceType.Ethernet || nic.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                    .Select(nic => nic)
                    .ToList()
                    .ForEach(nic => {
                        var item = nic.GetPhysicalAddress().GetAddressBytes();
                        if (item.Length == 6)
                        {
                            var s = MacAddressHelpers.MacAddressToString(MacAddressHelpers.ReverseMacAddress(item));
                            _logger.LogInformation("Adding MAC Address {MacAddress} from network interfaces", s);
                            _macAddresses.Add(s);
                        }
                    });

            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Exception occurred");
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var ml = new MethodLogger(_logger);

            long throttleTime = _configuration.GetValue<long>("UdpService:ThrottleTimeInSeconds", 5);
            long macAdressesRefreshTime = _configuration.GetValue<long>("UdpService:MacAddressesRefreshTimeInMinutes", 15);
            Timer macAddressesRefreshTimer = new Timer(InitializeMacAddresses, null, 0, macAdressesRefreshTime*60*1000);
            Timer sleepTimer = new Timer(async (o) => await _sleepService.ExecuteSleepCommand(stoppingToken), null, Timeout.Infinite, Timeout.Infinite);

            int udpPort = _configuration.GetValue<int>("UdpService:Port", 9);
            using UdpClient listener = new UdpClient(udpPort);
            _logger.LogInformation("Started listining on UDP port {UdpPort}", udpPort);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = await listener.ReceiveAsync().WithCancellation(stoppingToken);
                    _logger.LogInformation("Received UDP packet from {UdpFrom} with length {UdpMessageLength}", result.RemoteEndPoint, result.Buffer.Length);
                    if (!(result.Buffer.Length != 102 || result.Buffer.Take(6).Any(x => x != 0xff)))
                    {
                        var macAddress = MacAddressHelpers.MacAddressToString(result.Buffer.Skip(6).Take(6).ToArray());
                        if (_macAddresses.Contains(macAddress))
                        {
                            _logger.LogInformation("Accepted MAC address {MacAddress}", macAddress);
                            sleepTimer.Change(throttleTime*1000, Timeout.Infinite);
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
