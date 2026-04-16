using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using dotnet_services_viewer.Application.Interfaces;
using dotnet_services_viewer.Infrastructure;
using dotnet_services_viewer.Hubs;
using Microsoft.EntityFrameworkCore;

namespace dotnet_services_viewer.Application.Services;

public class MonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MonitoringService> _logger;
    private readonly IHubContext<MonitoringHub> _hubContext;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(10);

    public MonitoringService(
        IServiceProvider serviceProvider, 
        ILogger<MonitoringService> logger,
        IHubContext<MonitoringHub> hubContext)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _hubContext = hubContext;
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
        var serviceManager = scope.ServiceProvider.GetRequiredService<IServiceManager>();
        var encryptionService = scope.ServiceProvider.GetRequiredService<IEncryptionService>();

        var nodes = await context.Nodes.Include(n => n.Services).ToListAsync();

        foreach (var node in nodes)
        {
            try
            {
                await serviceManager.UpdateServicesStatusAsync(node);
                
                string? decryptedPassword = null;
                if (!string.IsNullOrEmpty(node.SshConfig.Password))
                {
                    decryptedPassword = encryptionService.Decrypt(node.SshConfig.Password);
                }

                string? decryptedPassphrase = null;
                if (!string.IsNullOrEmpty(node.SshConfig.Passphrase))
                {
                    decryptedPassphrase = encryptionService.Decrypt(node.SshConfig.Passphrase);
                }

                var cpuLoadStr = await sshClient.ExecuteCommandAsync(
                    node.IpAddress, 
                    node.SshConfig.Username, 
                    node.SshConfig.Port, 
                    "awk '{print $1}' /proc/loadavg", 
                    node.SshConfig.PrivateKeyPath,
                    decryptedPassword,
                    decryptedPassphrase);

                if (double.TryParse(cpuLoadStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double loadAvg))
                {
                    var cpuPercentage = Math.Min(loadAvg * 100 / 4, 100);

                    await _hubContext.Clients.All.SendAsync("ReceiveNodeUpdate", new {
                        nodeId = node.Id,
                        hostname = node.Hostname,
                        cpuUsage = cpuPercentage,
                        status = "Up",
                        services = node.Services.Select(s => new {
                            s.Id,
                            s.Name,
                            s.Status,
                            s.CurrentCpuUsage
                        })
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking node {Hostname}", node.Hostname);
                await _hubContext.Clients.All.SendAsync("ReceiveNodeUpdate", new {
                    nodeId = node.Id,
                    hostname = node.Hostname,
                    status = "Down",
                    error = ex.Message
                });
            }
        }

        await context.SaveChangesAsync();
    }
}
