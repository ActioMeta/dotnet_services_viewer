namespace dotnet_services_viewer.Domain;

public class Container
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; private set; } = "Unknown";
    public double CpuThreshold { get; set; } = 80.0;
    public double CurrentCpuUsage { get; private set; }

    public void UpdateStatus(double cpuUsage)
    {
        CurrentCpuUsage = cpuUsage;

        if (cpuUsage >= CpuThreshold)
        {
            Status = "Warning";
        }
        else
        {
            Status = "Up";
        }
    }
}
