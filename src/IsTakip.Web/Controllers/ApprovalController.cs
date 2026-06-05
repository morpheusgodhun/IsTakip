using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class ApprovalController : Controller
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly INotificationService _notify;

    public ApprovalController(AppDbContext db, ICurrentUserService currentUser, INotificationService notify)
    {
        _db = db;
        _currentUser = currentUser;
        _notify = notify;
    }

    private long TenantId => _currentUser.TenantId ?? 0;
    private long UserId => _currentUser.UserId ?? 0;

    // ---- Onay süreci başlat ----
    [HttpGet]
    public async Task<IActionResult> Start(long workItemId)
    {
        var wi = await _db.WorkItems.Where(w => w.Id == workItemId)
            .Select(w => new { w.Id, w.Key, w.Title }).FirstOrDefaultAsync();
        if (wi is null) return NotFound();

        return View(new ApprovalStartVM
        {
            WorkItemId = wi.Id,
            WorkItemKey = wi.Key,
            Title = wi.Title,
            ApproverUserIds = new List<long?> { null, null, null, null, null },
            Users = await UserOptions()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(ApprovalStartVM model)
    {
        var approvers = (model.ApproverUserIds ?? new()).Where(x => x.HasValue).Select(x => x!.Value).ToList();
        if (approvers.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "En az bir onaycı seçmelisiniz.");
            model.Users = await UserOptions();
            return View(model);
        }

        var wi = await _db.WorkItems.Where(w => w.Id == model.WorkItemId)
            .Select(w => new { w.Id, w.Key, w.Title }).FirstOrDefaultAsync();
        if (wi is null) return NotFound();

        var request = new ApprovalRequest
        {
            TenantId = TenantId,
            WorkItemId = model.WorkItemId,
            Status = ApprovalStatus.Beklemede,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.ApprovalRequests.Add(request);
        await _db.SaveChangesAsync();

        int order = 1;
        foreach (var uid in approvers)
            _db.ApprovalSteps.Add(new ApprovalStep { ApprovalRequestId = request.Id, StepOrder = order++, ApproverUserId = uid, Status = ApprovalStepStatus.Beklemede });
        await _db.SaveChangesAsync();

        // İlk onaycıya bildir.
        await _notify.NotifyAsync(approvers[0], "Onayınız bekleniyor",
            $"{wi.Key} · {wi.Title}", $"/WorkItems/Details/{wi.Id}", NotificationType.Sistem);

        TempData["Success"] = "Onay süreci başlatıldı.";
        return RedirectToAction("Details", "WorkItems", new { id = model.WorkItemId });
    }

    // ---- Onaylarım ----
    public async Task<IActionResult> Index()
    {
        // Bekleyen tüm adımları çek, ardından her istek için aktif (en küçük sıralı bekleyen)
        // adımı bul ve yalnızca bana ait olan aktif adımları göster.
        var pending = await (
            from s in _db.ApprovalSteps
            join r in _db.ApprovalRequests on s.ApprovalRequestId equals r.Id
            join w in _db.WorkItems on r.WorkItemId equals w.Id
            where s.Status == ApprovalStepStatus.Beklemede && r.Status == ApprovalStatus.Beklemede
            select new { s.Id, s.StepOrder, s.ApproverUserId, s.ApprovalRequestId, WorkItemId = w.Id, w.Key, w.Title, r.CreatedAtUtc })
            .ToListAsync();

        var activeForRequest = pending.GroupBy(x => x.ApprovalRequestId)
            .ToDictionary(g => g.Key, g => g.Min(x => x.StepOrder));

        var mine = pending
            .Where(x => x.ApproverUserId == UserId && x.StepOrder == activeForRequest[x.ApprovalRequestId])
            .Select(x => new MyApprovalRowVM
            {
                StepId = x.Id, WorkItemId = x.WorkItemId, WorkItemKey = x.Key,
                Title = x.Title, StepOrder = x.StepOrder, RequestedAtUtc = x.CreatedAtUtc
            })
            .OrderBy(x => x.RequestedAtUtc)
            .ToList();

        return View(mine);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(long stepId, bool approve, string? comment)
    {
        var step = await _db.ApprovalSteps.FirstOrDefaultAsync(s => s.Id == stepId);
        if (step is null) return NotFound();
        if (step.ApproverUserId != UserId) return Forbid();
        if (step.Status != ApprovalStepStatus.Beklemede)
        {
            TempData["Error"] = "Bu adım zaten karara bağlanmış.";
            return RedirectToAction(nameof(Index));
        }

        var request = await _db.ApprovalRequests.FirstAsync(r => r.Id == step.ApprovalRequestId);

        // Aktif adım mı? Önünde bekleyen daha küçük sıralı adım olmamalı.
        bool isActive = !await _db.ApprovalSteps.AnyAsync(s =>
            s.ApprovalRequestId == request.Id && s.Status == ApprovalStepStatus.Beklemede && s.StepOrder < step.StepOrder);
        if (!isActive)
        {
            TempData["Error"] = "Önceki onaylar tamamlanmadan bu adıma karar veremezsiniz.";
            return RedirectToAction(nameof(Index));
        }

        var wi = await _db.WorkItems.Where(w => w.Id == request.WorkItemId)
            .Select(w => new { w.Id, w.Key, w.Title, w.ReporterUserId }).FirstAsync();

        step.Comment = comment;
        step.DecidedAtUtc = DateTime.UtcNow;

        if (!approve)
        {
            step.Status = ApprovalStepStatus.Reddedildi;
            request.Status = ApprovalStatus.Reddedildi;
            await _db.SaveChangesAsync();
            if (wi.ReporterUserId is { } rep)
                await _notify.NotifyAsync(rep, "Onay reddedildi", $"{wi.Key} · {wi.Title}", $"/WorkItems/Details/{wi.Id}", NotificationType.Sistem);
            TempData["Success"] = "Reddedildi.";
            return RedirectToAction(nameof(Index));
        }

        step.Status = ApprovalStepStatus.Onaylandi;
        await _db.SaveChangesAsync();

        var next = await _db.ApprovalSteps
            .Where(s => s.ApprovalRequestId == request.Id && s.Status == ApprovalStepStatus.Beklemede)
            .OrderBy(s => s.StepOrder).FirstOrDefaultAsync();

        if (next is not null)
        {
            await _notify.NotifyAsync(next.ApproverUserId, "Onayınız bekleniyor",
                $"{wi.Key} · {wi.Title}", $"/WorkItems/Details/{wi.Id}", NotificationType.Sistem);
        }
        else
        {
            request.Status = ApprovalStatus.Onaylandi;
            await _db.SaveChangesAsync();
            if (wi.ReporterUserId is { } rep)
                await _notify.NotifyAsync(rep, "Onay süreci tamamlandı", $"{wi.Key} · {wi.Title} onaylandı.", $"/WorkItems/Details/{wi.Id}", NotificationType.Sistem);
        }

        TempData["Success"] = "Onaylandı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<SelectListItem>> UserOptions() =>
        await _db.Users.Where(u => u.TenantId == TenantId && u.Status == UserStatus.Aktif)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem(u.FirstName + " " + u.LastName, u.Id.ToString())).ToListAsync();
}
