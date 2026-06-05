using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class UsersController : Controller
{
    private const string ManagePermission = "User.Manage";

    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public UsersController(UserManager<AppUser> userManager, AppDbContext db, ICurrentUserService currentUser)
    {
        _userManager = userManager;
        _db = db;
        _currentUser = currentUser;
    }

    private long TenantId => _currentUser.TenantId ?? 0;

    // Yetki kontrolü: User.Manage izni yoksa erişim engellenir.
    private IActionResult? Guard() =>
        _currentUser.HasPermission(ManagePermission) ? null : RedirectToAction("AccessDenied", "Account");

    public async Task<IActionResult> Index()
    {
        if (Guard() is { } denied) return denied;

        // AppUser tenant global filtresine dahil değil; kiracıya göre elle filtreliyoruz.
        var rows = await _db.Users.AsNoTracking()
            .Where(u => u.TenantId == TenantId)
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.UserName,
                u.Email,
                u.Title,
                DepartmentName = u.Department != null ? u.Department.Name : null,
                u.Status
            })
            .ToListAsync();

        var ids = rows.Select(r => r.Id).ToList();
        var roleRows = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId)
            select new { ur.UserId, r.Name }).ToListAsync();

        string StatusText(UserStatus s) => s switch
        {
            UserStatus.Aktif => "Aktif",
            UserStatus.Pasif => "Pasif",
            UserStatus.Izinli => "İzinli",
            _ => "İşten Ayrıldı"
        };

        var users = rows.Select(u => new UserListItemVM
        {
            Id = u.Id,
            FullName = $"{u.FirstName} {u.LastName}",
            UserName = u.UserName!,
            Email = u.Email!,
            Title = u.Title,
            DepartmentName = u.DepartmentName,
            Status = StatusText(u.Status),
            Roles = string.Join(", ", roleRows.Where(m => m.UserId == u.Id).Select(m => m.Name))
        }).ToList();

        return View(users);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (Guard() is { } denied) return denied;
        var vm = new CreateUserViewModel();
        await FillLookupsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (Guard() is { } denied) return denied;

        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(model);
            return View(model);
        }

        var user = new AppUser
        {
            TenantId = TenantId,
            UserName = model.UserName.Trim(),
            Email = model.Email.Trim(),
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Title = model.Title,
            DepartmentId = model.DepartmentId,
            Status = UserStatus.Aktif,
            EmailConfirmed = true,
            LockoutEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = _currentUser.UserId
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            await FillLookupsAsync(model);
            return View(model);
        }

        if (model.RoleId is { } roleId)
        {
            var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId && r.TenantId == TenantId);
            if (role is not null) await _userManager.AddToRoleAsync(user, role.Name!);
        }

        TempData["Success"] = $"Kullanıcı oluşturuldu: {user.UserName}";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(long id)
    {
        if (Guard() is { } denied) return denied;

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id && u.TenantId == TenantId);
        if (user is null) return NotFound();

        return View(new ResetPasswordViewModel { UserId = user.Id, FullName = user.FirstName + " " + user.LastName });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (Guard() is { } denied) return denied;
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == model.UserId && u.TenantId == TenantId);
        if (user is null) return NotFound();

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError(string.Empty, e.Description);
            return View(model);
        }

        TempData["Success"] = $"{model.FullName} kullanıcısının parolası sıfırlandı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillLookupsAsync(CreateUserViewModel vm)
    {
        vm.Departments = await _db.Departments.Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToListAsync();

        vm.Roles = await _db.Roles.Where(r => r.TenantId == TenantId)
            .OrderBy(r => r.Name)
            .Select(r => new SelectListItem(r.Name, r.Id.ToString())).ToListAsync();
    }
}
