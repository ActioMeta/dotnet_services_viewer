using Xunit;
using dotnet_services_viewer.Domain;

namespace dotnet_services_viewer.Tests.Domain
{
    public class MonitoredServiceTests
    {
        [Fact]
        public void Service_ShouldBeInWarningState_WhenCpuUsageExceedsThreshold()
        {
            // Arrange
            var service = new MonitoredService { Name = "Web-App", CpuThreshold = 80.0 };
            
            // Act
            service.UpdateStatus(cpuUsage: 85.0, status: "Up");
            
            // Assert
            Assert.Equal("Warning", service.Status);
        }
        
        [Fact]
        public void Service_ShouldBeInUpState_WhenCpuUsageIsNormal()
        {
            // Arrange
            var service = new MonitoredService { Name = "Web-App", CpuThreshold = 80.0 };
            
            // Act
            service.UpdateStatus(cpuUsage: 15.0, status: "Up");
            
            // Assert
            Assert.Equal("Up", service.Status);
        }
    }
}
