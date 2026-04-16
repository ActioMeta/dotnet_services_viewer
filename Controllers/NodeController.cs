using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dotnet_services_viewer.Infrastructure;
using dotnet_services_viewer.Domain;
using dotnet_services_viewer.Application.Interfaces;

namespace dotnet_services_viewer.Controllers;

public class NodeController : Controller
{
    private readonly AppDbContext _context;
    private readonly IEncryptionService _encryptionService;

    public NodeController(AppDbContext context, IEncryptionService encryptionService)
    {
        _context = context;
        _encryptionService = encryptionService;
    }

    public async Task<IActionResult> Index()
    {
        var nodes = await _context.Nodes
            .Include(n => n.Services)
            .ToListAsync();
        return View(nodes);
    }

    public async Task<IActionResult> Details(int id)
    {
        var node = await _context.Nodes
            .Include(n => n.Services)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (node == null) return NotFound();

        return View(node);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Node node)
    {
        if (ModelState.IsValid)
        {
            if (!string.IsNullOrEmpty(node.SshConfig.Password))
            {
                node.SshConfig.Password = _encryptionService.Encrypt(node.SshConfig.Password);
            }
            if (!string.IsNullOrEmpty(node.SshConfig.Passphrase))
            {
                node.SshConfig.Passphrase = _encryptionService.Encrypt(node.SshConfig.Passphrase);
            }
            
            _context.Nodes.Add(node);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(node);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var node = await _context.Nodes.FindAsync(id);
        if (node == null) return NotFound();
        
        node.SshConfig.Password = null; 
        node.SshConfig.Passphrase = null; 
        return View(node);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Node node)
    {
        if (id != node.Id) return NotFound();

        if (ModelState.IsValid)
        {
            try
            {
                var existingNode = await _context.Nodes.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
                
                // Manejo de Password
                if (string.IsNullOrEmpty(node.SshConfig.Password))
                {
                    node.SshConfig.Password = existingNode?.SshConfig.Password;
                }
                else
                {
                    node.SshConfig.Password = _encryptionService.Encrypt(node.SshConfig.Password);
                }

                // Manejo de Passphrase
                if (string.IsNullOrEmpty(node.SshConfig.Passphrase))
                {
                    node.SshConfig.Passphrase = existingNode?.SshConfig.Passphrase;
                }
                else
                {
                    node.SshConfig.Passphrase = _encryptionService.Encrypt(node.SshConfig.Passphrase);
                }

                _context.Update(node);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Nodes.AnyAsync(e => e.Id == node.Id)) return NotFound();
                else throw;
            }
            return RedirectToAction(nameof(Index));
        }
        return View(node);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var node = await _context.Nodes.FindAsync(id);
        if (node != null)
        {
            _context.Nodes.Remove(node);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
