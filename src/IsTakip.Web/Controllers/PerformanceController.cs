using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class PerformanceController : Controller
{
    private const string ViewPermission = "Report.View";

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public PerformanceController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        if (!_currentUser.HasPermission(ViewPermission))
            return RedirectToAction("AccessDenied", "Account");

        // ✔ DateOnly standardı
        var nowDate = DateOnly.FromDateTime(DateTime.UtcNow);

        var tenantId = _currentUser.TenantId ?? 0;

        var data = await _db.WorkItems.AsNoTracking()
            .Where(w => w.AssigneeUserId != null)
            .Select(w => new
            {
                UserId = w.AssigneeUserId!.Value,

                IsDone = w.CurrentState.Category == StateCategory.Tamamlandi,

                // ✔ FIX: DateOnly karşılaştırma
                Overdue = w.DueDate != null
                       && w.DueDate.Value < nowDate
                       && w.CurrentState.Category != StateCategory.Tamamlandi
            })
            .ToListAsync();

        var names = await _db.Users
            .Where(u => u.TenantId == tenantId)
            .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

        var rows = data
            .GroupBy(x => x.UserId)
            .Select(g =>
            {
                int total = g.Count();
                int completed = g.Count(x => x.IsDone);
                int overdue = g.Count(x => x.Overdue);
                int pending = total - completed;

                return new PerformanceRowVM
                {
                    UserId = g.Key,
                    UserName = names.TryGetValue(g.Key, out var n) ? n : $"#{g.Key}",
                    Total = total,
                    Completed = completed,
                    Pending = pending,
                    Overdue = overdue,
                    Score = completed * 10 - overdue * 5
                };
            })
            .OrderByDescending(r => r.Score)
            .ToList();

        return View(rows);
    }
}