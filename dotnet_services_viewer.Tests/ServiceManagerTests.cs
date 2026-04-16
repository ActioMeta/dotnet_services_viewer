using Xunit;
using Moq;
using dotnet_services_viewer.Application.Interfaces;
using dotnet_services_viewer.Infrastructure.SSH;
using dotnet_services_viewer.Domain;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;

namespace dotnet_services_viewer.Tests;

public class ServiceManagerTests
{
    private readonly Mock<ISshClient> _mockSshClient;
    private readonly Mock<IEncryptionService> _mockEncryptionService;
    private readonly ServiceManager _serviceManager;

    public ServiceManagerTests()
    {
        _mockSshClient = new Mock<ISshClient>();
        _mockEncryptionService = new Mock<IEncryptionService>();
        // We'll need a way to mock HttpClient for WebApi tests, 
        // but let's start with SSH based ones first.
        _serviceManager = new ServiceManager(_mockSshClient.Object, _mockEncryptionService.Object);
    }

    [Fact]
    public async Task UpdateStatus_Docker_ShouldExecuteSpecificDockerStats()
    {
        // Arrange
        var node = new Node { IpAddress = "1.2.3.4", SshConfig = new SshConfig { Username = "user" } };
        var service = new MonitoredService { 
            Type = ServiceType.Docker, 
            Identifier = "my-db", 
            Name = "Database" 
        };
        node.Services.Add(service);

        _mockSshClient.Setup(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
            It.Is<string>(cmd => cmd.Contains("docker ps")), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("my-db|running");

        _mockSshClient.Setup(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
            It.Is<string>(cmd => cmd.Contains("docker stats") && cmd.Contains("my-db")), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("my-db|5.5");

        // Act
        await _serviceManager.UpdateServicesStatusAsync(node);

        // Assert
        Assert.Equal("Up", service.Status);
        Assert.Equal(5.5, service.CurrentCpuUsage);
    }

    [Fact]
    public async Task UpdateStatus_Quadlet_ShouldUseSystemctlAndPodmanStats()
    {
        // Arrange
        var node = new Node { IpAddress = "1.2.3.4", SshConfig = new SshConfig { Username = "user" } };
        var service = new MonitoredService { 
            Type = ServiceType.Quadlet, 
            Identifier = "my-app.service", 
            Name = "App" 
        };
        node.Services.Add(service);

        // Check if systemd service is active
        _mockSshClient.Setup(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
            It.Is<string>(cmd => cmd.Contains("systemctl is-active")), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("active");

        // Get stats via podman (Quadlets usually name the container the same as the service or we need a mapping)
        // For now assume Identifier is the service name and podman container name is derived or same.
        _mockSshClient.Setup(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
            It.Is<string>(cmd => cmd.Contains("podman stats")), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("my-app.service|10.2");

        // Act
        await _serviceManager.UpdateServicesStatusAsync(node);

        // Assert
        Assert.Equal("Up", service.Status);
        Assert.Equal(10.2, service.CurrentCpuUsage);
    }

    [Fact]
    public async Task DiscoverServices_Docker_ShouldParseOutputCorrectly()
    {
        // Arrange
        var node = new Node { IpAddress = "1.2.3.4", SshConfig = new SshConfig { Username = "user" } };
        _mockSshClient.Setup(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 
            It.Is<string>(cmd => cmd.Contains("docker ps -a")), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("web-app|nginx:latest|running\ndb-server|postgres:15|exited");

        // Act
        var results = await _serviceManager.DiscoverServicesAsync(node, ServiceType.Docker);

        // Assert
        Assert.Equal(2, results.Count());
        var web = results.First(r => r.Identifier == "web-app");
        Assert.Equal("web-app", web.Name);
        Assert.Equal("Up", web.Status);
    }
}
