using IsTakip.Application.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class RolesController : Controller
{
    private const string ManagePermission = "Role.Manage";

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RolesController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private long TenantId => _currentUser.TenantId ?? 0;
    private IActionResult? Guard() =>
        _currentUser.HasPermission(ManagePermission) ? null : RedirectToAction("AccessDenied", "Account");

    public async Task<IActionResult> Index()
    {
        if (Guard() is { } d) return d;

        var roles = await _db.Roles.AsNoTracking()
            .Where(r => r.TenantId == TenantId)
            .OrderBy(r => r.Name)
            .Select(r => new RoleListItemVM
            {
                Id = r.Id,
                Name = r.Name!,
                Description = r.Description,
                IsSystem = r.IsSystem,
                PermissionCount = _db.RolePermissions.Count(rp => rp.RoleId == r.Id),
                MemberCount = _db.UserRoles.Count(ur => ur.RoleId == r.Id)
            })
            .ToListAsync();

        return View(roles);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (Guard() is { } d) return d;
        return View("Edit", new RoleEditVM { Groups = await BuildGroupsAsync() });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        if (Guard() is { } d) return d;

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == TenantId);
        if (role is null) return NotFound();

        var vm = new RoleEditVM
        {
            Id = role.Id,
            Name = role.Name!,
            Description = role.Description,
            IsSystem = role.IsSystem,
            Groups = await BuildGroupsAsync(),
            SelectedPermissionIds = await _db.RolePermissions
                .Where(rp => rp.RoleId == role.Id).Select(rp => rp.PermissionId).ToListAsync()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(RoleEditVM model)
    {
        if (Guard() is { } d) return d;

        if (string.IsNullOrWhiteSpace(model.Name))
            ModelState.AddModelError(nameof(model.Name), "Rol adı zorunludur.");

        if (!ModelState.IsValid)
        {
            model.Groups = await BuildGroupsAsync();
            return View(model);
        }

        AppRole role;
        if (model.Id == 0)
        {
            role = new AppRole
            {
                TenantId = TenantId,
                IsSystem = false,
                CreatedAtUtc = DateTime.UtcNow,
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };
            _db.Roles.Add(role);
        }
        else
        {
            role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == model.Id && r.TenantId == TenantId)
                   ?? throw new InvalidOperationException("Rol bulunamadı.");
        }

        role.Name = model.Name.Trim();
        role.NormalizedName = model.Name.Trim().ToUpperInvariant();
        role.Description = model.Description;
        await _db.SaveChangesAsync();

        // İzinleri yeniden yaz (mevcutları sil, seçilenleri ekle).
        var existing = await _db.RolePermissions.Where(rp => rp.RoleId == role.Id).ToListAsync();
        _db.RolePermissions.RemoveRange(existing);
        foreach (var pid in model.SelectedPermissionIds.Distinct())
            _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = pid });
        await _db.SaveChangesAsync();

        TempData["Success"] = model.Id == 0 ? "Rol oluşturuldu." : "Rol güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Copy(long id)
    {
        if (Guard() is { } d) return d;

        var source = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == TenantId);
        if (source is null) return NotFound();

        var clone = new AppRole
        {
            TenantId = TenantId,
            Name = source.Name + " (Kopya)",
            NormalizedName = (source.Name + " (Kopya)").ToUpperInvariant(),
            Description = source.Description,
            IsSystem = false,
            CreatedAtUtc = DateTime.UtcNow,
            ConcurrencyStamp = Guid.NewGuid().ToString()
        };
        _db.Roles.Add(clone);
        await _db.SaveChangesAsync();

        var perms = await _db.RolePermissions.Where(rp => rp.RoleId == id).Select(rp => rp.PermissionId).ToListAsync();
        foreach (var pid in perms)
            _db.RolePermissions.Add(new RolePermission { RoleId = clone.Id, PermissionId = pid });
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Rol kopyalandı: {clone.Name}";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        if (Guard() is { } d) return d;

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == id && r.TenantId == TenantId);
        if (role is null) return NotFound();
        if (role.IsSystem)
        {
            TempData["Error"] = "Sistem rolü silinemez.";
            return RedirectToAction(nameof(Index));
        }

        var userRoles = await _db.UserRoles.Where(ur => ur.RoleId == id).ToListAsync();
        _db.UserRoles.RemoveRange(userRoles);
        var rolePerms = await _db.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
        _db.RolePermissions.RemoveRange(rolePerms);
        _db.Roles.Remove(role); // interceptor soft-delete uygular
        await _db.SaveChangesAsync();

        TempData["Success"] = "Rol silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<PermissionGroupVM>> BuildGroupsAsync()
    {
        var perms = await _db.Permissions.AsNoTracking()
            .OrderBy(p => p.SortOrder)
            .Select(p => new { p.Id, p.Key, p.Module, p.DisplayName })
            .ToListAsync();

        return perms.GroupBy(p => p.Module)
            .Select(g => new PermissionGroupVM
            {
                Module = g.Key,
                Permissions = g.Select(p => new PermissionOptionVM
                {
                    Id = p.Id,
                    Key = p.Key,
                    DisplayName = p.DisplayName
                }).ToList()
            }).ToList();
    }
}
