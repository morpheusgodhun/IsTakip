using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    // Dashboard widget kataloğu (sıralanabilir / açılıp kapatılabilir bloklar).
    public static readonly string[] AllWidgets = { "ozet", "durum", "gecikenler", "son" };

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public HomeController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private string LayoutKey => $"dashboard.layout.{_currentUser.UserId ?? 0}";

    public async Task<IActionResult> Index()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var uid = _currentUser.UserId ?? 0;

        var vm = new DashboardVM
        {
            Open = await _db.WorkItems.CountAsync(w => w.CompletedAtUtc == null),
            Done = await _db.WorkItems.CountAsync(w => w.CompletedAtUtc != null),
            Overdue = await _db.WorkItems.CountAsync(w => w.CompletedAtUtc == null && w.DueDate != null && w.DueDate < today),
            MyOpen = await _db.WorkItems.CountAsync(w => w.AssigneeUserId == uid && w.CompletedAtUtc == null),
            Todo = await _db.WorkItems.CountAsync(w => w.CurrentState.Category == StateCategory.Yapilacak),
            InProgress = await _db.WorkItems.CountAsync(w => w.CurrentState.Category == StateCategory.DevamEdiyor),
            DoneCat = await _db.WorkItems.CountAsync(w => w.CurrentState.Category == StateCategory.Tamamlandi)
        };

        vm.Recent = await _db.WorkItems.AsNoTracking()
            .OrderByDescending(w => w.Id).Take(6)
            .Select(w => new DashboardItem
            {
                Id = w.Id, Key = w.Key, Title = w.Title,
                StateName = w.CurrentState.Name, StateColor = w.CurrentState.ColorHex, DueDate = w.DueDate
            }).ToListAsync();

        vm.OverdueItems = await _db.WorkItems.AsNoTracking()
            .Where(w => w.CompletedAtUtc == null && w.DueDate != null && w.DueDate < today)
            .OrderBy(w => w.DueDate).Take(6)
            .Select(w => new DashboardItem
            {
                Id = w.Id, Key = w.Key, Title = w.Title,
                StateName = w.CurrentState.Name, StateColor = w.CurrentState.ColorHex, DueDate = w.DueDate
            }).ToListAsync();

        // Bekleyen onaylarım (aktif adım bende olanlar).
        var pending = await (
            from s in _db.ApprovalSteps
            join r in _db.ApprovalRequests on s.ApprovalRequestId equals r.Id
            where s.Status == ApprovalStepStatus.Beklemede && r.Status == ApprovalStatus.Beklemede
            select new { s.ApproverUserId, s.StepOrder, s.ApprovalRequestId }).ToListAsync();
        var activeForReq = pending.GroupBy(x => x.ApprovalRequestId).ToDictionary(g => g.Key, g => g.Min(x => x.StepOrder));
        vm.MyApprovals = pending.Count(x => x.ApproverUserId == uid && x.StepOrder == activeForReq[x.ApprovalRequestId]);

        // Kişiye özel dashboard düzeni (SystemSetting'te saklanır).
        var saved = await _db.SystemSettings.Where(s => s.Key == LayoutKey).Select(s => s.Value).FirstOrDefaultAsync();
        vm.Layout = !string.IsNullOrWhiteSpace(saved)
            ? saved.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Where(k => AllWidgets.Contains(k)).ToList()
            : AllWidgets.ToList();

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLayout(string? widgets)
    {
        var keys = (widgets ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(k => AllWidgets.Contains(k))
            .Distinct()
            .ToList();

        var tenantId = _currentUser.TenantId ?? 0;
        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == LayoutKey);
        if (setting is null)
        {
            setting = new SystemSetting { TenantId = tenantId, Key = LayoutKey, CreatedAtUtc = DateTime.UtcNow };
            _db.SystemSettings.Add(setting);
        }
        setting.Value = string.Join(",", keys);
        setting.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Panel düzeni kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public IActionResult Error() => View();
}
