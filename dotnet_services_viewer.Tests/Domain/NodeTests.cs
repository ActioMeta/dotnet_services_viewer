using Xunit;
using System.Collections.Generic;
using dotnet_services_viewer.Domain;

namespace dotnet_services_viewer.Tests.Domain
{
    public class NodeTests
    {
        [Fact]
        public void Node_ShouldContainListOfContainers()
        {
            // Arrange
            var node = new Node 
            { 
                Hostname = "production-server", 
                IpAddress = "192.168.1.100" 
            };
            
            var container1 = new Container { Name = "App-1" };
            var container2 = new Container { Name = "App-2" };

            // Act
            node.Containers.Add(container1);
            node.Containers.Add(container2);

            // Assert
            Assert.Equal(2, node.Containers.Count);
            Assert.Contains(container1, node.Containers);
            Assert.Contains(container2, node.Containers);
        }

        [Fact]
        public void Node_ShouldHaveDefaultSshConfig()
        {
            // Arrange
            var node = new Node();

            // Assert
            Assert.NotNull(node.SshConfig);
            Assert.Equal("root", node.SshConfig.Username);
            Assert.Equal(22, node.SshConfig.Port);
        }
    }
}
