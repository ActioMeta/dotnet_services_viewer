using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using dotnet_services_viewer.Application.Services;
using dotnet_services_viewer.Application.Interfaces;
using dotnet_services_viewer.Infrastructure;
using dotnet_services_viewer.Domain;
using dotnet_services_viewer.Hubs;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace dotnet_services_viewer.Tests.Services;

public class MonitoringServiceTests
{
    private AppDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task PerformHealthChecks_ShouldSendUpdate_WhenNodeIsUp()
    {
        // Arrange
        var context = GetDatabaseContext();
        var node = new Node { Id = 1, Hostname = "test", IpAddress = "127.0.0.1" };
        context.Nodes.Add(node);
        await context.SaveChangesAsync();

        var mockSsh = new Mock<ISshClient>();
        mockSsh.Setup(s => s.ExecuteCommandAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
               .ReturnsAsync("0.50"); // Simula loadavg

        var mockHub = new Mock<IHubContext<MonitoringHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);

        var mockEncryption = new Mock<IEncryptionService>();
        
        var services = new ServiceCollection();
        services.AddSingleton(context);
        services.AddSingleton(mockSsh.Object);
        services.AddSingleton(mockEncryption.Object);
        var serviceProvider = services.BuildServiceProvider();

        var logger = new Mock<ILogger<MonitoringService>>().Object;
        var service = new MonitoringService(serviceProvider, logger, mockHub.Object);

        // Act
        // Accedemos al método privado via reflexión para testing o lo hacemos público/internal
        var method = typeof(MonitoringService).GetMethod("PerformHealthChecks", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method.Invoke(service, null);

        // Assert
        mockClientProxy.Verify(
            c => c.SendCoreAsync("ReceiveNodeUpdate", It.Is<object[]>(o => o.Length > 0), default),
            Times.Once);
    }
}
