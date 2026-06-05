using IsTakip.Application.Common;
using IsTakip.Application.WorkItems;
using IsTakip.Domain.Common;
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
            IsManager = _currentUser.HasPermission(ManagerPermission),
            Subtasks = await _workItems.GetSubtasksAsync(id),
            CustomFields = await (
                from v in _db.WorkItemCustomFieldValues.AsNoTracking()
                join def in _db.CustomFieldDefinitions on v.CustomFieldDefinitionId equals def.Id
                where v.WorkItemId == id && v.ValueText != null && v.ValueText != ""
                orderby def.SortOrder
                select new CustomFieldValueVM { Name = def.Name, Value = v.ValueText! }).ToListAsync()
        };

        // Onay süreci bilgisi (varsa en güncel istek).
        var req = await _db.ApprovalRequests.AsNoTracking()
            .Where(r => r.WorkItemId == id)
            .OrderByDescending(r => r.Id)
            .Select(r => new { r.Id, r.Status })
            .FirstOrDefaultAsync();
        if (req is not null)
        {
            var steps = await (
                from s in _db.ApprovalSteps.AsNoTracking()
                join u in _db.Users on s.ApproverUserId equals u.Id
                where s.ApprovalRequestId == req.Id
                orderby s.StepOrder
                select new { s.Id, s.StepOrder, s.Status, s.Comment, s.DecidedAtUtc, Name = u.FirstName + " " + u.LastName, s.ApproverUserId }
            ).ToListAsync();

            int? activeOrder = steps.Where(s => s.Status == ApprovalStepStatus.Beklemede)
                .Select(s => (int?)s.StepOrder).Min();

            vm.Approval = new ApprovalInfoVM
            {
                RequestId = req.Id,
                StatusText = ApprovalStatusText(req.Status),
                StatusColor = ApprovalStatusColor(req.Status),
                Steps = steps.Select(s => new ApprovalStepVM
                {
                    StepId = s.Id,
                    Order = s.StepOrder,
                    ApproverName = s.Name,
                    StatusText = StepStatusText(s.Status),
                    StatusColor = StepStatusColor(s.Status),
                    Comment = s.Comment,
                    DecidedAtUtc = s.DecidedAtUtc,
                    IsMyActiveStep = s.Status == ApprovalStepStatus.Beklemede
                                     && s.StepOrder == activeOrder
                                     && s.ApproverUserId == (_currentUser.UserId ?? 0)
                }).ToList()
            };
        }

        // Ekler (dosyalar).
        var atts = await _db.Attachments.AsNoTracking()
            .Where(a => a.WorkItemId == id)
            .OrderByDescending(a => a.UploadedAtUtc)
            .Select(a => new { a.Id, a.FileName, a.SizeBytes, a.UploadedByUserId, a.UploadedAtUtc })
            .ToListAsync();
        var uploaderIds = atts.Where(a => a.UploadedByUserId != null).Select(a => a.UploadedByUserId!.Value).Distinct().ToList();
        var uploaderNames = await _db.Users.Where(u => uploaderIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);
        vm.Attachments = atts.Select(a => new AttachmentVM
        {
            Id = a.Id,
            FileName = a.FileName,
            SizeText = SlaHelper.SizeText(a.SizeBytes),
            UploaderName = a.UploadedByUserId != null && uploaderNames.TryGetValue(a.UploadedByUserId.Value, out var n) ? n : null,
            UploadedAtUtc = a.UploadedAtUtc
        }).ToList();

        // SLA göstergesi (son tarih + tamamlanma).
        var done = await _db.WorkItems.Where(w => w.Id == id).Select(w => w.CompletedAtUtc != null).FirstOrDefaultAsync();
        var (slaText, slaColor) = SlaHelper.Status(item.DueDate, done);
        vm.SlaText = slaText;
        vm.SlaColor = slaColor;

        return View(vm);
    }

    private static string ApprovalStatusText(ApprovalStatus s) => s switch
    {
        ApprovalStatus.Beklemede => "Onay Bekliyor",
        ApprovalStatus.Onaylandi => "Onaylandı",
        ApprovalStatus.Reddedildi => "Reddedildi",
        _ => "İptal"
    };
    private static string ApprovalStatusColor(ApprovalStatus s) => s switch
    {
        ApprovalStatus.Onaylandi => "#22A06B",
        ApprovalStatus.Reddedildi => "#C9372C",
        ApprovalStatus.Beklemede => "#E2B203",
        _ => "#5E6C84"
    };
    private static string StepStatusText(ApprovalStepStatus s) => s switch
    {
        ApprovalStepStatus.Onaylandi => "Onayladı",
        ApprovalStepStatus.Reddedildi => "Reddetti",
        _ => "Bekliyor"
    };
    private static string StepStatusColor(ApprovalStepStatus s) => s switch
    {
        ApprovalStepStatus.Onaylandi => "#22A06B",
        ApprovalStepStatus.Reddedildi => "#C9372C",
        _ => "#E2B203"
    };

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
    public async Task<IActionResult> Create(long? parentId, long? typeId)
    {
        var vm = new CreateWorkItemViewModel();
        if (typeId is { } tid2) vm.WorkItemTypeId = tid2;
        if (parentId is { } pid)
        {
            var parent = await _db.WorkItems.Where(w => w.Id == pid)
                .Select(w => new { w.Id, w.Key }).FirstOrDefaultAsync();
            if (parent is not null)
            {
                vm.ParentWorkItemId = parent.Id;
                vm.ParentKey = parent.Key;
            }
        }
        await FillLookupsAsync(vm);
        vm.CustomFieldInputs = await LoadCustomFieldInputsAsync();
        return View(vm);
    }

    private async Task<List<CustomFieldInputVM>> LoadCustomFieldInputsAsync()
    {
        var defs = await _db.CustomFieldDefinitions.AsNoTracking()
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name).ToListAsync();
        if (defs.Count == 0) return new();

        var ids = defs.Select(d => d.Id).ToList();
        var opts = await _db.CustomFieldOptions.AsNoTracking()
            .Where(o => ids.Contains(o.CustomFieldDefinitionId))
            .OrderBy(o => o.SortOrder).ToListAsync();

        return defs.Select(d => new CustomFieldInputVM
        {
            Id = d.Id, Name = d.Name, FieldType = d.FieldType, IsRequired = d.IsRequired,
            Options = opts.Where(o => o.CustomFieldDefinitionId == d.Id).Select(o => o.Value).ToList()
        }).ToList();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateWorkItemViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await FillLookupsAsync(model);
            model.CustomFieldInputs = await LoadCustomFieldInputsAsync();
            return View(model);
        }

        var result = await _workItems.CreateAsync(new CreateWorkItemRequest
        {
            WorkItemTypeId = model.WorkItemTypeId,
            ParentWorkItemId = model.ParentWorkItemId,
            Title = model.Title,
            Description = model.Description,
            PriorityId = model.PriorityId,
            AssigneeUserId = model.AssigneeUserId,
            DepartmentId = model.DepartmentId,
            DueDate = model.DueDate,
            CustomFields = model.CustomFields
        });

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Kayıt oluşturulamadı.");
            await FillLookupsAsync(model);
            model.CustomFieldInputs = await LoadCustomFieldInputsAsync();
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
