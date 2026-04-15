using Renci.SshNet;
using dotnet_services_viewer.Application.Interfaces;

namespace dotnet_services_viewer.Infrastructure.SSH
{
    public class SshService : dotnet_services_viewer.Application.Interfaces.ISshClient
    {
        public async Task<string> ExecuteCommandAsync(string hostname, string username, int port, string command, string? privateKeyPath = null)
        {
            return await Task.Run(() =>
            {
                Renci.SshNet.ConnectionInfo connectionInfo;
                if (!string.IsNullOrEmpty(privateKeyPath))
                {
                    var keyFile = new PrivateKeyFile(privateKeyPath);
                    connectionInfo = new Renci.SshNet.ConnectionInfo(hostname, port, username, new PrivateKeyAuthenticationMethod(username, keyFile));
                }
                else
                {
                    // For simplicity, we assume passwordless SSH or an agent is handled outside.
                    // In a real app, you'd handle passwords or keys more robustly.
                    connectionInfo = new Renci.SshNet.ConnectionInfo(hostname, port, username, new NoneAuthenticationMethod(username));
                }

                using var client = new Renci.SshNet.SshClient(connectionInfo);
                client.Connect();
                var cmd = client.CreateCommand(command);
                var result = cmd.Execute();
                client.Disconnect();
                return result;
            });
        }
    }
}
