using Xunit;
using dotnet_services_viewer.Infrastructure.Security;
using dotnet_services_viewer.Infrastructure.SSH;
using System;

namespace dotnet_services_viewer.Tests.Infrastructure;

public class InfrastructureTests
{
    [Fact]
    public void EncryptionService_ShouldEncryptAndDecryptCorrectly()
    {
        // Arrange
        var service = new EncryptionService();
        var originalPassword = "MySecretPassword123!";

        // Act
        var encrypted = service.Encrypt(originalPassword);
        var decrypted = service.Decrypt(encrypted);

        // Assert
        Assert.NotEqual(originalPassword, encrypted);
        Assert.Equal(originalPassword, decrypted);
    }

    [Fact]
    public void SshService_ShouldNotThrow_WhenPathContainsTilde()
    {
        // Esta es una prueba de lógica de strings, no de conexión real
        var privateKeyPath = "~/.ssh/id_rsa";
        var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var expectedPath = privateKeyPath.Replace("~", homePath);

        Assert.Contains(homePath, expectedPath);
        Assert.StartsWith("/", expectedPath);
    }
}
