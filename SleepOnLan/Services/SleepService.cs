using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SleepOnLan
{
    public interface ISleepService
    {
        Task<bool> ExecuteSleepCommand(CancellationToken stoppingToken);
    }
    public class SleepService : ISleepService
    {
        private readonly ILogger<SleepService> _logger;
        private readonly IConfiguration _configuration;

        public SleepService(ILogger<SleepService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<bool> ExecuteSleepCommand(CancellationToken stoppingToken)
        {
            var osPlatform =
                (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" :
                (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "Linux" :
                (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "OSX" :
                "Unknown")));

            var enabled = _configuration.GetValue<bool>("SleepService:Enabled", false);
            var commandInfo = _configuration
                .GetSection($"Commands:{osPlatform}")
                .Get<CommandInfo>() ?? new CommandInfo() { Command = "cmd.exe", Arguments = "/c echo No command specified in SleepOnLan configuration" };

            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.CreateNoWindow = true;
            processStartInfo.FileName = commandInfo.Command;
            processStartInfo.Arguments = commandInfo.Arguments;
            processStartInfo.RedirectStandardOutput = true;
            processStartInfo.RedirectStandardError = true;
            processStartInfo.UseShellExecute = false;

            var outputs = new System.Collections.Generic.List<string>();
            var errors = new System.Collections.Generic.List<string>();
            using var process = new System.Diagnostics.Process();
            process.StartInfo = processStartInfo;
            process.OutputDataReceived += (sender, args) => outputs.Add(args.Data ?? "");
            process.ErrorDataReceived += (sender, args) => errors.Add(args.Data ?? "");

            if (!enabled)
            {
                _logger.LogInformation("Sleep service is not enabled in config, sleep command {SleepCommand} with {SleepCommandArguments} not executed", commandInfo.Command, commandInfo.Arguments);
            }
            else
            {
                _logger.LogInformation("Starting sleep command {SleepCommand} with {SleepCommandArguments}", commandInfo.Command, commandInfo.Arguments);
                await Task.Delay(500);
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                await process.WaitForExitAsync(stoppingToken);
                var output = string.Join("\r\n", outputs);
                int exitCode = process.ExitCode;
                if (exitCode != 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
