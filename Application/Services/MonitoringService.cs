using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using dotnet_services_viewer.Application.Interfaces;
using dotnet_services_viewer.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace dotnet_services_viewer.Application.Services
{
    public class MonitoringService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MonitoringService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

        public MonitoringService(IServiceProvider serviceProvider, ILogger<MonitoringService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Monitoring Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await PerformHealthChecks();
                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Monitoring Service is stopping.");
        }

        private async Task PerformHealthChecks()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var sshClient = scope.ServiceProvider.GetRequiredService<ISshClient>();

            var nodes = await context.Nodes.Include(n => n.Containers).ToListAsync();

            foreach (var node in nodes)
            {
                foreach (var container in node.Containers)
                {
                    try
                    {
                        // Simulate or run a command like 'docker stats --no-stream --format "{{.CPUPerc}}"'
                        // For now, we'll just simulate a CPU reading or run a generic command.
                        var output = await sshClient.ExecuteCommandAsync(
                            node.Hostname, 
                            node.SshConfig.Username, 
                            node.SshConfig.Port, 
                            "uptime", // Generic command for testing connectivity
                            node.SshConfig.PrivateKeyPath);

                        _logger.LogInformation("Node {Hostname} responded: {Output}", node.Hostname, output.Trim());
                        
                        // Update container status (mocking logic for now)
                        var randomCpu = new Random().NextDouble() * 100;
                        container.UpdateStatus(randomCpu);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error checking node {Hostname}", node.Hostname);
                    }
                }
            }

            await context.SaveChangesAsync();
        }
    }
}
