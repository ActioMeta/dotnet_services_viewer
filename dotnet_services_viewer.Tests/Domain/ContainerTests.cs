using Xunit;
using System.Collections.Generic;
using dotnet_services_viewer.Domain;

namespace dotnet_services_viewer.Tests.Domain
{
    public class ContainerTests
    {
        [Fact]
        public void Container_ShouldBeInWarningState_WhenCpuUsageExceedsThreshold()
        {
            // Arrange
            var container = new Container { Name = "Web-App", CpuThreshold = 80.0 };
            
            // Act
            container.UpdateStatus(cpuUsage: 85.0); // Supera el 80%
            
            // Assert
            Assert.Equal("Warning", container.Status);
        }
        
        [Fact]
        public void Container_ShouldBeInUpState_WhenCpuUsageIsNormal()
        {
            // Arrange
            var container = new Container { Name = "Web-App", CpuThreshold = 80.0 };
            
            // Act
            container.UpdateStatus(cpuUsage: 15.0); // Por debajo del 80%
            
            // Assert
            Assert.Equal("Up", container.Status);
        }
    }

    // Nota: El test fallará en compilación porque aún no existe la clase Container.
    // Esto es parte del ciclo TDD: Red -> Green -> Refactor.
}
