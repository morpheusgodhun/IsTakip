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
public class OrganizationController : Controller
{
    private const string ManagePermission = "Organization.Manage";

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public OrganizationController(AppDbContext db, ICurrentUserService currentUser)
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

        var companies = await _db.Companies.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        var branches = await _db.Branches.AsNoTracking().OrderBy(b => b.Name).ToListAsync();
        var depts = await _db.Departments.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
        var teams = await _db.Teams.AsNoTracking().OrderBy(t => t.Name).ToListAsync();

        var userNames = await _db.Users.Where(u => u.TenantId == TenantId)
            .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);
        var teamCounts = await _db.TeamMembers.GroupBy(m => m.TeamId)
            .Select(g => new { TeamId = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.TeamId, x => x.Count);

        var vm = new OrgTreeVM
        {
            Companies = companies.Select(c => new OrgCompany
            {
                Id = c.Id, Name = c.Name, TaxNumber = c.TaxNumber, IsActive = c.IsActive,
                Branches = branches.Where(b => b.CompanyId == c.Id).Select(b => new OrgBranch
                {
                    Id = b.Id, Name = b.Name,
                    Departments = depts.Where(d => d.BranchId == b.Id).Select(d => new OrgDepartment
                    {
                        Id = d.Id, Name = d.Name,
                        ManagerName = d.ManagerUserId.HasValue && userNames.ContainsKey(d.ManagerUserId.Value)
                            ? userNames[d.ManagerUserId.Value] : null,
                        Teams = teams.Where(t => t.DepartmentId == d.Id).Select(t => new OrgTeam
                        {
                            Id = t.Id, Name = t.Name,
                            LeadName = t.LeadUserId.HasValue && userNames.ContainsKey(t.LeadUserId.Value)
                                ? userNames[t.LeadUserId.Value] : null,
                            MemberCount = teamCounts.TryGetValue(t.Id, out var cnt) ? cnt : 0
                        }).ToList()
                    }).ToList()
                }).ToList()
            }).ToList()
        };
        return View(vm);
    }

    // -------------------- Şirket --------------------
    [HttpGet] public IActionResult Company() { if (Guard() is { } d) return d; return View(new CompanyFormVM()); }

    [HttpGet]
    public async Task<IActionResult> CompanyEdit(long id)
    {
        if (Guard() is { } d) return d;
        var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
        if (c is null) return NotFound();
        return View("Company", new CompanyFormVM { Id = c.Id, Name = c.Name, TaxNumber = c.TaxNumber, IsActive = c.IsActive });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Company(CompanyFormVM m)
    {
        if (Guard() is { } d) return d;
        if (string.IsNullOrWhiteSpace(m.Name)) ModelState.AddModelError(nameof(m.Name), "Ad zorunludur.");
        if (!ModelState.IsValid) return View(m);

        Company c;
        if (m.Id == 0) { c = new Company { TenantId = TenantId }; _db.Companies.Add(c); }
        else c = await _db.Companies.FirstAsync(x => x.Id == m.Id);
        c.Name = m.Name.Trim(); c.TaxNumber = m.TaxNumber; c.IsActive = m.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Şirket kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompanyDelete(long id)
    {
        if (Guard() is { } d) return d;
        var c = await _db.Companies.FirstOrDefaultAsync(x => x.Id == id);
        if (c is not null) { _db.Companies.Remove(c); await _db.SaveChangesAsync(); TempData["Success"] = "Şirket silindi."; }
        return RedirectToAction(nameof(Index));
    }

    // -------------------- Şube --------------------
    [HttpGet]
    public async Task<IActionResult> Branch(long? id)
    {
        if (Guard() is { } d) return d;
        var vm = new BranchFormVM();
        if (id is { } bid)
        {
            var b = await _db.Branches.FirstOrDefaultAsync(x => x.Id == bid);
            if (b is null) return NotFound();
            vm = new BranchFormVM { Id = b.Id, Name = b.Name, CompanyId = b.CompanyId, LocationId = b.LocationId, IsActive = b.IsActive };
        }
        await FillBranchLookups(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Branch(BranchFormVM m)
    {
        if (Guard() is { } d) return d;
        if (string.IsNullOrWhiteSpace(m.Name)) ModelState.AddModelError(nameof(m.Name), "Ad zorunludur.");
        if (m.CompanyId == 0) ModelState.AddModelError(nameof(m.CompanyId), "Şirket seçilmelidir.");
        if (!ModelState.IsValid) { await FillBranchLookups(m); return View(m); }

        Branch b;
        if (m.Id == 0) { b = new Branch { TenantId = TenantId }; _db.Branches.Add(b); }
        else b = await _db.Branches.FirstAsync(x => x.Id == m.Id);
        b.Name = m.Name.Trim(); b.CompanyId = m.CompanyId; b.LocationId = m.LocationId; b.IsActive = m.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Şube kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BranchDelete(long id)
    {
        if (Guard() is { } d) return d;
        var b = await _db.Branches.FirstOrDefaultAsync(x => x.Id == id);
        if (b is not null) { _db.Branches.Remove(b); await _db.SaveChangesAsync(); TempData["Success"] = "Şube silindi."; }
        return RedirectToAction(nameof(Index));
    }

    // -------------------- Departman --------------------
    [HttpGet]
    public async Task<IActionResult> Department(long? id)
    {
        if (Guard() is { } d) return d;
        var vm = new DepartmentFormVM();
        if (id is { } did)
        {
            var dep = await _db.Departments.FirstOrDefaultAsync(x => x.Id == did);
            if (dep is null) return NotFound();
            vm = new DepartmentFormVM { Id = dep.Id, Name = dep.Name, BranchId = dep.BranchId, ManagerUserId = dep.ManagerUserId, IsActive = dep.IsActive };
        }
        await FillDeptLookups(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Department(DepartmentFormVM m)
    {
        if (Guard() is { } d) return d;
        if (string.IsNullOrWhiteSpace(m.Name)) ModelState.AddModelError(nameof(m.Name), "Ad zorunludur.");
        if (!ModelState.IsValid) { await FillDeptLookups(m); return View(m); }

        Department dep;
        if (m.Id == 0) { dep = new Department { TenantId = TenantId }; _db.Departments.Add(dep); }
        else dep = await _db.Departments.FirstAsync(x => x.Id == m.Id);
        dep.Name = m.Name.Trim(); dep.BranchId = m.BranchId; dep.ManagerUserId = m.ManagerUserId; dep.IsActive = m.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Departman kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DepartmentDelete(long id)
    {
        if (Guard() is { } d) return d;
        var dep = await _db.Departments.FirstOrDefaultAsync(x => x.Id == id);
        if (dep is not null) { _db.Departments.Remove(dep); await _db.SaveChangesAsync(); TempData["Success"] = "Departman silindi."; }
        return RedirectToAction(nameof(Index));
    }

    // -------------------- Takım --------------------
    [HttpGet]
    public async Task<IActionResult> Team(long? id)
    {
        if (Guard() is { } d) return d;
        var vm = new TeamFormVM();
        if (id is { } tid)
        {
            var t = await _db.Teams.FirstOrDefaultAsync(x => x.Id == tid);
            if (t is null) return NotFound();
            vm = new TeamFormVM { Id = t.Id, Name = t.Name, DepartmentId = t.DepartmentId, LeadUserId = t.LeadUserId };
        }
        await FillTeamLookups(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Team(TeamFormVM m)
    {
        if (Guard() is { } d) return d;
        if (string.IsNullOrWhiteSpace(m.Name)) ModelState.AddModelError(nameof(m.Name), "Ad zorunludur.");
        if (!ModelState.IsValid) { await FillTeamLookups(m); return View(m); }

        Team t;
        if (m.Id == 0) { t = new Team { TenantId = TenantId }; _db.Teams.Add(t); }
        else t = await _db.Teams.FirstAsync(x => x.Id == m.Id);
        t.Name = m.Name.Trim(); t.DepartmentId = m.DepartmentId; t.LeadUserId = m.LeadUserId;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Takım kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TeamDelete(long id)
    {
        if (Guard() is { } d) return d;
        var t = await _db.Teams.FirstOrDefaultAsync(x => x.Id == id);
        if (t is not null) { _db.Teams.Remove(t); await _db.SaveChangesAsync(); TempData["Success"] = "Takım silindi."; }
        return RedirectToAction(nameof(Index));
    }

    // -------------------- Lookups --------------------
    private async Task<List<SelectListItem>> UserOptions() =>
        await _db.Users.Where(u => u.TenantId == TenantId && u.Status == UserStatus.Aktif)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem(u.FirstName + " " + u.LastName, u.Id.ToString())).ToListAsync();

    private async Task FillBranchLookups(BranchFormVM vm)
    {
        vm.Companies = await _db.Companies.OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync();
        vm.Locations = await _db.Locations.OrderBy(l => l.Name)
            .Select(l => new SelectListItem(l.Name, l.Id.ToString())).ToListAsync();
    }

    private async Task FillDeptLookups(DepartmentFormVM vm)
    {
        vm.Branches = await _db.Branches.OrderBy(b => b.Name)
            .Select(b => new SelectListItem(b.Name, b.Id.ToString())).ToListAsync();
        vm.Users = await UserOptions();
    }

    private async Task FillTeamLookups(TeamFormVM vm)
    {
        vm.Departments = await _db.Departments.OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToListAsync();
        vm.Users = await UserOptions();
    }
}
