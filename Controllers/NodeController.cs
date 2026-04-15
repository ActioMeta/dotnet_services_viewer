using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using dotnet_services_viewer.Infrastructure;
using dotnet_services_viewer.Domain;

namespace dotnet_services_viewer.Controllers
{
    public class NodeController : Controller
    {
        private readonly AppDbContext _context;

        public NodeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var nodes = await _context.Nodes
                .Include(n => n.Containers)
                .ToListAsync();
            return View(nodes);
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
                _context.Nodes.Add(node);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(node);
        }
    }
}
