using Xunit;
using System.Collections.Generic;
using dotnet_services_viewer.Domain;

namespace dotnet_services_viewer.Tests.Domain
{
    public class NodeTests
    {
        [Fact]
        public void Node_ShouldBeAbleToAddServices()
        {
            // Arrange
            var node = new Node { Hostname = "TestNode", IpAddress = "192.168.1.10" };
            var service1 = new MonitoredService { Name = "Web" };
            var service2 = new MonitoredService { Name = "DB" };
            
            // Act
            node.Services.Add(service1);
            node.Services.Add(service2);
            
            // Assert
            Assert.Equal(2, node.Services.Count);
            Assert.Contains(service1, node.Services);
            Assert.Contains(service2, node.Services);
        }
    }
}
