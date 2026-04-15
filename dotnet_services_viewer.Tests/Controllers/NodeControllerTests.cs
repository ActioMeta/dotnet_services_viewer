using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dotnet_services_viewer.Controllers;
using dotnet_services_viewer.Infrastructure;
using dotnet_services_viewer.Domain;
using dotnet_services_viewer.Infrastructure.Security;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace dotnet_services_viewer.Tests.Controllers;

public class NodeControllerTests
{
    private AppDbContext GetDatabaseContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        var databaseContext = new AppDbContext(options);
        databaseContext.Database.EnsureCreated();
        return databaseContext;
    }

    [Fact]
    public async Task Index_ReturnsAViewResult_WithAListOfNodes()
    {
        // Arrange
        var context = GetDatabaseContext();
        var encryptionService = new EncryptionService();
        var controller = new NodeController(context, encryptionService);

        // Act
        var result = await controller.Index();

        // Assert
        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsAssignableFrom<IEnumerable<Node>>(viewResult.ViewData.Model);
    }

    [Fact]
    public async Task Create_RedirectsToIndex_WhenModelStateIsValid()
    {
        // Arrange
        var context = GetDatabaseContext();
        var encryptionService = new EncryptionService();
        var controller = new NodeController(context, encryptionService);
        var node = new Node { Hostname = "test-node", IpAddress = "127.0.0.1" };

        // Act
        var result = await controller.Create(node);

        // Assert
        var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirectToActionResult.ActionName);
    }

    [Fact]
    public async Task Edit_UpdatesNodeAndEncryptsNewPassword()
    {
        // Arrange
        var context = GetDatabaseContext();
        var encryptionService = new EncryptionService();
        var node = new Node { Hostname = "Old Name", IpAddress = "1.1.1.1" };
        context.Nodes.Add(node);
        await context.SaveChangesAsync();
        
        var controller = new NodeController(context, encryptionService);
        node.Hostname = "New Name";
        node.SshConfig.Password = "new-password";

        // Act
        var result = await controller.Edit(node.Id, node);

        // Assert
        var updatedNode = await context.Nodes.FindAsync(node.Id);
        Assert.Equal("New Name", updatedNode.Hostname);
        Assert.Equal(encryptionService.Encrypt("new-password"), updatedNode.SshConfig.Password);
    }

    [Fact]
    public async Task Delete_RemovesNodeFromDatabase()
    {
        // Arrange
        var context = GetDatabaseContext();
        var node = new Node { Hostname = "To Delete", IpAddress = "0.0.0.0" };
        context.Nodes.Add(node);
        await context.SaveChangesAsync();
        var controller = new NodeController(context, new EncryptionService());

        // Act
        await controller.DeleteConfirmed(node.Id);

        // Assert
        var deletedNode = await context.Nodes.FindAsync(node.Id);
        Assert.Null(deletedNode);
    }
}
