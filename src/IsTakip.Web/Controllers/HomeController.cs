using IsTakip.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        // Basit yönetici paneli sayaçları (global tenant filtresi otomatik uygulanır).
        ViewBag.OpenCount = await _db.WorkItems.CountAsync(w => w.CompletedAtUtc == null);
        ViewBag.DoneCount = await _db.WorkItems.CountAsync(w => w.CompletedAtUtc != null);
        ViewBag.TypeCount = await _db.WorkItemTypes.CountAsync();
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
