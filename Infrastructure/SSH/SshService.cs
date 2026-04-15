using Renci.SshNet;
using dotnet_services_viewer.Application.Interfaces;

namespace dotnet_services_viewer.Infrastructure.SSH;

public class SshService : dotnet_services_viewer.Application.Interfaces.ISshClient
{
    public async Task<string> ExecuteCommandAsync(string hostname, string username, int port, string command, string? privateKeyPath = null, string? password = null, string? passphrase = null)
    {
        return await Task.Run(() =>
        {
            Renci.SshNet.ConnectionInfo connectionInfo;

            if (!string.IsNullOrEmpty(privateKeyPath))
            {
                string actualPath = privateKeyPath;
                if (privateKeyPath.StartsWith("~"))
                {
                    var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    actualPath = privateKeyPath.Replace("~", homePath);
                }

                PrivateKeyFile keyFile;
                if (!string.IsNullOrEmpty(passphrase))
                {
                    keyFile = new PrivateKeyFile(actualPath, passphrase);
                }
                else
                {
                    keyFile = new PrivateKeyFile(actualPath);
                }

                connectionInfo = new Renci.SshNet.ConnectionInfo(hostname, port, username, new PrivateKeyAuthenticationMethod(username, keyFile));
            }
            else if (!string.IsNullOrEmpty(password))
            {
                connectionInfo = new Renci.SshNet.ConnectionInfo(hostname, port, username, new PasswordAuthenticationMethod(username, password));
            }
            else
            {
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
