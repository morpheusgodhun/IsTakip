using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

/// <summary>
/// İK / Satın Alma talepleri. Ayrı tablo yoktur; talepler ilgili görev türünde
/// kayıt olarak açılır (Form Tasarımcısı alanlarıyla) ve Onay Motoru ile yürür.
/// </summary>
[Authorize]
public class TalepController : Controller
{
    private static readonly string[] RequestTypeNames =
        { "İzin Talebi", "Avans Talebi", "Fazla Mesai Talebi", "Satın Alma Talebi" };

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public TalepController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index()
    {
        var uid = _currentUser.UserId ?? 0;

        var types = await _db.WorkItemTypes.AsNoTracking()
            .Where(t => t.IsActive && RequestTypeNames.Contains(t.Name))
            .OrderBy(t => t.Name)
            .Select(t => new TalepTypeVM { Id = t.Id, Name = t.Name, Color = t.ColorHex })
            .ToListAsync();

        var typeIds = types.Select(t => t.Id).ToList();

        var rows = await _db.WorkItems.AsNoTracking()
            .Where(w => typeIds.Contains(w.WorkItemTypeId) && w.ReporterUserId == uid)
            .OrderByDescending(w => w.Id)
            .Select(w => new TalepRowVM
            {
                Id = w.Id,
                Key = w.Key,
                Title = w.Title,
                TypeName = w.WorkItemType.Name,
                StateName = w.CurrentState.Name,
                StateColor = w.CurrentState.ColorHex,
                CreatedAtUtc = w.CreatedAtUtc
            })
            .ToListAsync();

        // Onay durumlarını ekle (en güncel onay isteğine göre).
        var ids = rows.Select(r => r.Id).ToList();
        var approvalsRaw = await _db.ApprovalRequests.AsNoTracking()
            .Where(a => ids.Contains(a.WorkItemId))
            .Select(a => new { a.WorkItemId, a.Id, a.Status })
            .ToListAsync();
        var amap = approvalsRaw
            .GroupBy(a => a.WorkItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.Id).First().Status);
        foreach (var r in rows)
        {
            if (amap.TryGetValue(r.Id, out var st))
            {
                r.ApprovalText = st switch
                {
                    ApprovalStatus.Beklemede => "Onay Bekliyor",
                    ApprovalStatus.Onaylandi => "Onaylandı",
                    ApprovalStatus.Reddedildi => "Reddedildi",
                    _ => "İptal"
                };
                r.ApprovalColor = st switch
                {
                    ApprovalStatus.Onaylandi => "#22A06B",
                    ApprovalStatus.Reddedildi => "#C9372C",
                    ApprovalStatus.Beklemede => "#E2B203",
                    _ => "#5E6C84"
                };
            }
            else
            {
                r.ApprovalText = "Onay başlatılmadı";
            }
        }

        return View(new TalepIndexVM { Types = types, MyRequests = rows });
    }
}
