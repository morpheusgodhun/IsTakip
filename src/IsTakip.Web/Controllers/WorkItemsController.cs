using IsTakip.Application.Common;
using IsTakip.Application.WorkItems;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class WorkItemsController : Controller
{
    private const string ManagerPermission = "WorkItem.Delete";

    private readonly IWorkItemService _workItems;
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public WorkItemsController(IWorkItemService workItems, AppDbContext db, ICurrentUserService currentUser)
    {
        _workItems = workItems;
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index([FromQuery] WorkItemFilter filter)
    {
        var result = await _workItems.GetListAsync(filter);
        var vm = new WorkItemListViewModel
        {
            Filter = filter,
            Items = result.Items,
            TotalCount = result.TotalCount,
            Result = new PagedResultView { Page = result.Page, PageSize = result.PageSize, TotalCount = result.TotalCount }
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(long id)
    {
        var item = await _workItems.GetByIdAsync(id);
        if (item is null) return NotFound();

        var vm = new WorkItemDetailsPageVM
        {
            Item = item,
            States = await _workItems.GetStatesAsync(id),
            Comments = await _workItems.GetCommentsAsync(id),
            IsManager = _currentUser.HasPermission(ManagerPermission)
        };
        return View(vm);
    }

    // Durum değiştirme — herkes Tamamlandı/kapanış dışındaki durumlara alabilir;
    // kapanış durumları servis tarafında yöneticilere kısıtlıdır.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeState(long id, long toStateId)
    {
        var result = await _workItems.ChangeStateAsync(id, toStateId);
        if (result.Succeeded) TempData["Success"] = "Durum güncellendi.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    // Yorum ekleme — giriş yapmış herkes yorum yazabilir.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(long id, string body)
    {
        var result = await _workItems.AddCommentAsync(id, body);
        if (!result.Succeeded) TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    // Silme — yalnızca yöneticiler.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _workItems.DeleteAsync(id);
        if (result.Succeeded)
        {
            TempData["Success"] = "Kayıt silindi.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var vm = new CreateWorkItemViewModel();
        await FillLookupsAsync(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWorkItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(model);
            return View(model);
        }

        var result = await _workItems.CreateAsync(new CreateWorkItemRequest
        {
            WorkItemTypeId = model.WorkItemTypeId,
            Title = model.Title,
            Description = model.Description,
            PriorityId = model.PriorityId,
            AssigneeUserId = model.AssigneeUserId,
            DepartmentId = model.DepartmentId,
            DueDate = model.DueDate
        });

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt oluşturulamadı.");
            await FillLookupsAsync(model);
            return View(model);
        }

        TempData["Success"] = "Kayıt oluşturuldu.";
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    private async Task FillLookupsAsync(CreateWorkItemViewModel vm)
    {
        vm.Types = await _db.WorkItemTypes.Where(t => t.IsActive)
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.Name, t.Id.ToString())).ToListAsync();

        vm.Priorities = await _db.Priorities.OrderBy(p => p.SortOrder)
            .Select(p => new SelectListItem(p.Name, p.Id.ToString())).ToListAsync();

        vm.Departments = await _db.Departments.Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new SelectListItem(d.Name, d.Id.ToString())).ToListAsync();

        vm.Users = await _db.Users.Where(u => u.Status == Domain.Common.UserStatus.Aktif)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem(u.FirstName + " " + u.LastName, u.Id.ToString())).ToListAsync();
    }
}
