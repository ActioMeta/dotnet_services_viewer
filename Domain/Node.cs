using System.Collections.Generic;

namespace dotnet_services_viewer.Domain;

public class Node
{
    public int Id { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public SshConfig SshConfig { get; set; } = new SshConfig();
    public List<Container> Containers { get; set; } = new List<Container>();
}

public class SshConfig
{
    public string Username { get; set; } = "root";
    public string? Password { get; set; }
    public string? Passphrase { get; set; } // For private keys
    public int Port { get; set; } = 22;
    public string? PrivateKeyPath { get; set; }
}

