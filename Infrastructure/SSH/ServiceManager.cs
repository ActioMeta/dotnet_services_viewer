using dotnet_services_viewer.Application.Interfaces;
using dotnet_services_viewer.Domain;
using System.Net.Http;

namespace dotnet_services_viewer.Infrastructure.SSH;

public class ServiceManager : IServiceManager
{
    private readonly ISshClient _sshClient;
    private readonly IEncryptionService _encryptionService;
    private readonly HttpClient _httpClient;

    public ServiceManager(ISshClient sshClient, IEncryptionService encryptionService)
    {
        _sshClient = sshClient;
        _encryptionService = encryptionService;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<bool> StartServiceAsync(Node node, MonitoredService service)
    {
        string command = service.Type switch
        {
            ServiceType.Docker => $"docker start {service.Identifier}",
            ServiceType.Podman => $"podman start {service.Identifier}",
            ServiceType.Quadlet or ServiceType.LinuxService => $"systemctl start {service.Identifier}",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(command)) return false;
        var result = await ExecuteSshCommandAsync(node, command);
        return !string.IsNullOrEmpty(result);
    }

    public async Task<bool> StopServiceAsync(Node node, MonitoredService service)
    {
        string command = service.Type switch
        {
            ServiceType.Docker => $"docker stop {service.Identifier}",
            ServiceType.Podman => $"podman stop {service.Identifier}",
            ServiceType.Quadlet or ServiceType.LinuxService => $"systemctl stop {service.Identifier}",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(command)) return false;
        var result = await ExecuteSshCommandAsync(node, command);
        return !string.IsNullOrEmpty(result);
    }

    public async Task<bool> RestartServiceAsync(Node node, MonitoredService service)
    {
        string command = service.Type switch
        {
            ServiceType.Docker => $"docker restart {service.Identifier}",
            ServiceType.Podman => $"podman restart {service.Identifier}",
            ServiceType.Quadlet or ServiceType.LinuxService => $"systemctl restart {service.Identifier}",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(command)) return false;
        var result = await ExecuteSshCommandAsync(node, command);
        return !string.IsNullOrEmpty(result);
    }

    public async Task UpdateServicesStatusAsync(Node node)
    {
        if (node.Services == null || !node.Services.Any()) return;

        foreach (var service in node.Services)
        {
            try
            {
                switch (service.Type)
                {
                    case ServiceType.Docker:
                        await UpdateDockerStatus(node, service);
                        break;
                    case ServiceType.Podman:
                        await UpdatePodmanStatus(node, service);
                        break;
                    case ServiceType.Quadlet:
                        await UpdateQuadletStatus(node, service);
                        break;
                    case ServiceType.LinuxService:
                        await UpdateLinuxServiceStatus(node, service);
                        break;
                    case ServiceType.WebApi:
                        await UpdateWebApiStatus(service);
                        break;
                }
            }
            catch
            {
                service.UpdateStatus(0, "Error");
            }
        }
    }

    public async Task<IEnumerable<MonitoredService>> DiscoverServicesAsync(Node node, ServiceType type)
    {
        var services = new List<MonitoredService>();
        string command = type switch
        {
            ServiceType.Docker => "docker ps -a --format \"{{.Names}}|{{.Image}}|{{.State}}\"",
            ServiceType.Podman => "podman ps -a --format \"{{.Names}}|{{.Image}}|{{.State}}\"",
            ServiceType.Quadlet or ServiceType.LinuxService => "systemctl list-units --type=service --all --no-legend --no-pager",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(command)) return services;

        var result = await ExecuteSshCommandAsync(node, command);
        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            if (type == ServiceType.Docker || type == ServiceType.Podman)
            {
                var parts = line.Split('|');
                if (parts.Length >= 3)
                {
                    services.Add(new MonitoredService {
                        Name = parts[0],
                        Identifier = parts[0],
                        Type = type,
                        Status = parts[2].Trim() == "running" ? "Up" : "Down"
                    });
                }
            }
            else if (type == ServiceType.LinuxService || type == ServiceType.Quadlet)
            {
                // Format: UNIT LOAD ACTIVE SUB DESCRIPTION
                // Example: docker.service loaded active running Docker Application Container Engine
                var parts = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    services.Add(new MonitoredService {
                        Name = parts[0].Replace(".service", ""),
                        Identifier = parts[0],
                        Type = type,
                        Status = parts[2].Trim() == "active" ? "Up" : "Down"
                    });
                }
            }
        }

        return services;
    }

    private async Task UpdateDockerStatus(Node node, MonitoredService service)
    {
        var psResult = await ExecuteSshCommandAsync(node, $"docker ps -a --filter \"name={service.Identifier}\" --format \"{{{{.Names}}}}|{{{{.State}}}}\"");
        var statsResult = await ExecuteSshCommandAsync(node, $"docker stats {service.Identifier} --no-stream --format \"{{{{.Name}}}}|{{{{.CPUPerc}}}}\"");
        
        ParseDockerPodmanResult(service, psResult, statsResult);
    }

    private async Task UpdatePodmanStatus(Node node, MonitoredService service)
    {
        var psResult = await ExecuteSshCommandAsync(node, $"podman ps -a --filter \"name={service.Identifier}\" --format \"{{{{.Names}}}}|{{{{.State}}}}\"");
        var statsResult = await ExecuteSshCommandAsync(node, $"podman stats {service.Identifier} --no-stream --format \"{{{{.Name}}}}|{{{{.CPUPerc}}}}\"");
        
        ParseDockerPodmanResult(service, psResult, statsResult);
    }

    private async Task UpdateQuadletStatus(Node node, MonitoredService service)
    {
        // Status from systemd
        var isActive = await ExecuteSshCommandAsync(node, $"systemctl is-active {service.Identifier}");
        string status = isActive.Trim() == "active" ? "Up" : "Down";

        // Metrics from podman (assuming container name matches service name or is derived)
        // Note: podman stats might need the container name, Quadlets often use the service name.
        var statsResult = await ExecuteSshCommandAsync(node, $"podman stats {service.Identifier} --no-stream --format \"{{{{.Name}}}}|{{{{.CPUPerc}}}}\"");
        
        double cpu = 0;
        if (statsResult.Contains("|"))
        {
            var parts = statsResult.Split('|');
            double.TryParse(parts[1].TrimEnd('%'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out cpu);
        }

        service.UpdateStatus(cpu, status);
    }

    private async Task UpdateLinuxServiceStatus(Node node, MonitoredService service)
    {
        var isActive = await ExecuteSshCommandAsync(node, $"systemctl is-active {service.Identifier}");
        string status = isActive.Trim() == "active" ? "Up" : "Down";
        
        double cpu = 0;
        if (status == "Up")
        {
            // Try to get CPU usage via systemd properties (CPUUsageNS is nanoseconds of CPU time)
            // This requires a delta, so for now we might use a simpler 'ps' or 'top' approach for a single snapshot
            // Alternative: use systemd-cgtop or parse /sys/fs/cgroup/system.slice/service/cpu.stat
            var cpuResult = await ExecuteSshCommandAsync(node, $"systemctl show {service.Identifier} --property=CPUUsageNS --value");
            if (double.TryParse(cpuResult, out double usageNs))
            {
                // To get percentage we'd need two samples. For a quick snapshot, let's use 'ps'
                var psCpu = await ExecuteSshCommandAsync(node, $"ps -o %cpu= -p $(systemctl show {service.Identifier} --property=MainPID --value)");
                double.TryParse(psCpu, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out cpu);
            }
        }

        service.UpdateStatus(cpu, status);
    }

    private async Task UpdateWebApiStatus(MonitoredService service)
    {
        try
        {
            var response = await _httpClient.GetAsync(service.Identifier);
            service.UpdateStatus(0, response.IsSuccessStatusCode ? "Up" : "Down");
        }
        catch
        {
            service.UpdateStatus(0, "Down");
        }
    }

    private void ParseDockerPodmanResult(MonitoredService service, string psResult, string statsResult)
    {
        string status = "Down";
        if (psResult.Contains("|"))
        {
            var parts = psResult.Split('|');
            status = parts[1].Trim() == "running" ? "Up" : "Down";
        }

        double cpu = 0;
        if (statsResult.Contains("|"))
        {
            var parts = statsResult.Split('|');
            double.TryParse(parts[1].TrimEnd('%'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out cpu);
        }

        service.UpdateStatus(cpu, status);
    }

    private async Task<string> ExecuteSshCommandAsync(Node node, string command)
    {
        string? decryptedPassword = null;
        if (!string.IsNullOrEmpty(node.SshConfig.Password))
            decryptedPassword = _encryptionService.Decrypt(node.SshConfig.Password);

        string? decryptedPassphrase = null;
        if (!string.IsNullOrEmpty(node.SshConfig.Passphrase))
            decryptedPassphrase = _encryptionService.Decrypt(node.SshConfig.Passphrase);

        return await _sshClient.ExecuteCommandAsync(
            node.IpAddress,
            node.SshConfig.Username,
            node.SshConfig.Port,
            command,
            node.SshConfig.PrivateKeyPath,
            decryptedPassword,
            decryptedPassphrase);
    }
}
