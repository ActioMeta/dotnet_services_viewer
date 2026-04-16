using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dotnet_services_viewer.Infrastructure;
using dotnet_services_viewer.Domain;
using dotnet_services_viewer.Application.Interfaces;

namespace dotnet_services_viewer.Controllers;

public class ServiceController : Controller
{
    private readonly AppDbContext _context;
    private readonly IServiceManager _serviceManager;

    public ServiceController(AppDbContext context, IServiceManager serviceManager)
    {
        _context = context;
        _serviceManager = serviceManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int nodeId, string name, string identifier, ServiceType type, double cpuThreshold)
    {
        var node = await _context.Nodes.Include(n => n.Services).FirstOrDefaultAsync(n => n.Id == nodeId);
        if (node == null) return NotFound();

        var service = new MonitoredService
        {
            Name = name,
            Identifier = identifier,
            Type = type,
            CpuThreshold = cpuThreshold,
            Status = "Unknown",
            NodeId = nodeId
        };

        node.Services.Add(service);
        await _context.SaveChangesAsync();

        return RedirectToAction("Details", "Node", new { id = nodeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int nodeId)
    {
        var service = await _context.Services.FindAsync(id);
        if (service != null)
        {
            _context.Services.Remove(service);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Details", "Node", new { id = nodeId });
    }

    [HttpPost]
    public async Task<IActionResult> Start(int id, int nodeId)
    {
        var node = await _context.Nodes.Include(n => n.Services).FirstOrDefaultAsync(n => n.Id == nodeId);
        var service = node?.Services.FirstOrDefault(s => s.Id == id);
        
        if (node == null || service == null) return NotFound();

        var success = await _serviceManager.StartServiceAsync(node, service);
        return Json(new { success });
    }

    [HttpPost]
    public async Task<IActionResult> Stop(int id, int nodeId)
    {
        var node = await _context.Nodes.Include(n => n.Services).FirstOrDefaultAsync(n => n.Id == nodeId);
        var service = node?.Services.FirstOrDefault(s => s.Id == id);
        
        if (node == null || service == null) return NotFound();

        var success = await _serviceManager.StopServiceAsync(node, service);
        return Json(new { success });
    }

    [HttpGet]
    public async Task<IActionResult> Discover(int nodeId, ServiceType type)
    {
        var node = await _context.Nodes.FindAsync(nodeId);
        if (node == null) return NotFound();

        var services = await _serviceManager.DiscoverServicesAsync(node, type);
        
        // Filter out already monitored services
        var existingIdentifiers = await _context.Services
            .Where(s => s.NodeId == nodeId && s.Type == type)
            .Select(s => s.Identifier)
            .ToListAsync();

        var newServices = services.Where(s => !existingIdentifiers.Contains(s.Identifier));

        return Json(newServices);
    }
}
