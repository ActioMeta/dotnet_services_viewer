using dotnet_services_viewer.Domain;

namespace dotnet_services_viewer.Application.Interfaces;

public interface IServiceManager
{
    Task<bool> StartServiceAsync(Node node, MonitoredService service);
    Task<bool> StopServiceAsync(Node node, MonitoredService service);
    Task<bool> RestartServiceAsync(Node node, MonitoredService service);
    Task UpdateServicesStatusAsync(Node node);
    Task<IEnumerable<MonitoredService>> DiscoverServicesAsync(Node node, ServiceType type);
}
