using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dotnet_services_viewer.Controllers;
using dotnet_services_viewer.Domain;
using dotnet_services_viewer.Infrastructure;
using Xunit;

namespace dotnet_services_viewer.Tests.Controllers
{
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
            using var context = GetDatabaseContext();
            context.Nodes.Add(new Node { Hostname = "Node 1", IpAddress = "127.0.0.1" });
            context.Nodes.Add(new Node { Hostname = "Node 2", IpAddress = "127.0.0.2" });
            await context.SaveChangesAsync();

            var controller = new NodeController(context);

            // Act
            var result = await controller.Index();

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<IEnumerable<Node>>(viewResult.ViewData.Model);
            Assert.Equal(2, model.Count());
        }

        [Fact]
        public async Task Create_Post_RedirectsToIndex_WhenModelIsValid()
        {
            // Arrange
            using var context = GetDatabaseContext();
            var controller = new NodeController(context);
            var newNode = new Node { Hostname = "New Node", IpAddress = "192.168.1.1" };

            // Act
            var result = await controller.Create(newNode);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            Assert.Equal(1, context.Nodes.Count());
        }
    }
}
