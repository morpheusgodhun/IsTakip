using IsTakip.Application.Common;
using IsTakip.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class NotificationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public NotificationsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private long UserId => _currentUser.UserId ?? 0;

    public async Task<IActionResult> Index()
    {
        var items = await _db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == UserId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(100)
            .ToListAsync();
        return View(items);
    }

    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var count = await _db.Notifications.CountAsync(n => n.RecipientUserId == UserId && !n.IsRead);
        return Json(new { count });
    }

    [HttpGet]
    public async Task<IActionResult> Recent()
    {
        var items = await _db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == UserId)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(10)
            .Select(n => new { n.Id, n.Title, n.Body, n.LinkUrl, n.IsRead, n.CreatedAtUtc })
            .ToListAsync();
        return Json(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead(long id)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.RecipientUserId == UserId);
        if (n is not null && !n.IsRead)
        {
            n.IsRead = true;
            await _db.SaveChangesAsync();
        }
        return Json(new { success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead()
    {
        var unread = await _db.Notifications.Where(n => n.RecipientUserId == UserId && !n.IsRead).ToListAsync();
        foreach (var n in unread) n.IsRead = true;
        if (unread.Count > 0) await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
