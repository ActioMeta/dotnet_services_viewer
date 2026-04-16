namespace dotnet_services_viewer.Domain;

public enum ServiceType
{
    Docker,
    Podman,
    Quadlet,
    LinuxService,
    WebApi
}

public class MonitoredService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Display Name
    public ServiceType Type { get; set; } = ServiceType.Docker;
    public string Identifier { get; set; } = string.Empty; // Docker Name, Systemd Service, or URL
    public string Status { get; set; } = "Unknown";
    public double CpuThreshold { get; set; } = 80.0;
    public double CurrentCpuUsage { get; set; }
    
    public int NodeId { get; set; }

    public void UpdateStatus(double cpuUsage, string status)
    {
        CurrentCpuUsage = cpuUsage;
        Status = status;

        if (status == "Up" && cpuUsage >= CpuThreshold)
        {
            Status = "Warning";
        }
    }
}
