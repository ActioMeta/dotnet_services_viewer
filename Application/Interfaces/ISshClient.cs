namespace dotnet_services_viewer.Application.Interfaces
{
    public interface ISshClient
    {
        Task<string> ExecuteCommandAsync(string hostname, string username, int port, string command, string? privateKeyPath = null);
    }
}
