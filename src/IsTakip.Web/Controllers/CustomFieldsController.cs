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
public class CustomFieldsController : Controller
{
    private const string ManagePermission = "Settings.Manage";

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CustomFieldsController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private long TenantId => _currentUser.TenantId ?? 0;
    private IActionResult? Guard() =>
        _currentUser.HasPermission(ManagePermission) ? null : RedirectToAction("AccessDenied", "Account");

    public async Task<IActionResult> Index()
    {
        if (Guard() is { } g) return g;

        var types = await _db.WorkItemTypes.ToDictionaryAsync(t => t.Id, t => t.Name);
        var defs = await _db.CustomFieldDefinitions.AsNoTracking()
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Name).ToListAsync();

        var rows = defs.Select(d => new CustomFieldListItemVM
        {
            Id = d.Id,
            Name = d.Name,
            FieldTypeName = FieldTypeName(d.FieldType),
            IsRequired = d.IsRequired,
            Scope = d.WorkItemTypeId.HasValue && types.ContainsKey(d.WorkItemTypeId.Value)
                ? types[d.WorkItemTypeId.Value] : "Tüm türler",
            OptionCount = _db.CustomFieldOptions.Count(o => o.CustomFieldDefinitionId == d.Id)
        }).ToList();

        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (Guard() is { } g) return g;
        return View("Edit", await FillAsync(new CustomFieldEditVM()));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        if (Guard() is { } g) return g;
        var d = await _db.CustomFieldDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (d is null) return NotFound();

        var options = await _db.CustomFieldOptions.Where(o => o.CustomFieldDefinitionId == id)
            .OrderBy(o => o.SortOrder).Select(o => o.Value).ToListAsync();

        var vm = new CustomFieldEditVM
        {
            Id = d.Id, Name = d.Name, FieldType = d.FieldType, IsRequired = d.IsRequired,
            WorkItemTypeId = d.WorkItemTypeId, SortOrder = d.SortOrder,
            OptionsText = string.Join("\n", options)
        };
        return View(await FillAsync(vm));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CustomFieldEditVM model)
    {
        if (Guard() is { } g) return g;
        if (string.IsNullOrWhiteSpace(model.Name)) ModelState.AddModelError(nameof(model.Name), "Alan adı zorunludur.");
        if (!ModelState.IsValid) return View(await FillAsync(model));

        CustomFieldDefinition d;
        if (model.Id == 0) { d = new CustomFieldDefinition { TenantId = TenantId }; _db.CustomFieldDefinitions.Add(d); }
        else d = await _db.CustomFieldDefinitions.FirstAsync(x => x.Id == model.Id);

        d.Name = model.Name.Trim();
        d.FieldType = model.FieldType;
        d.IsRequired = model.IsRequired;
        d.WorkItemTypeId = model.WorkItemTypeId;
        d.SortOrder = model.SortOrder;
        await _db.SaveChangesAsync();

        // Seçenekleri yeniden yaz (yalnızca seçim türlerinde).
        var existing = await _db.CustomFieldOptions.Where(o => o.CustomFieldDefinitionId == d.Id).ToListAsync();
        _db.CustomFieldOptions.RemoveRange(existing);
        if (model.FieldType is CustomFieldType.Secim or CustomFieldType.CokluSecim && !string.IsNullOrWhiteSpace(model.OptionsText))
        {
            int i = 0;
            foreach (var line in model.OptionsText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                _db.CustomFieldOptions.Add(new CustomFieldOption { CustomFieldDefinitionId = d.Id, Value = line, SortOrder = i++ });
        }
        await _db.SaveChangesAsync();

        TempData["Success"] = "Alan kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        if (Guard() is { } g) return g;
        var d = await _db.CustomFieldDefinitions.FirstOrDefaultAsync(x => x.Id == id);
        if (d is not null) { _db.CustomFieldDefinitions.Remove(d); await _db.SaveChangesAsync(); TempData["Success"] = "Alan silindi."; }
        return RedirectToAction(nameof(Index));
    }

    private async Task<CustomFieldEditVM> FillAsync(CustomFieldEditVM vm)
    {
        vm.WorkItemTypes = await _db.WorkItemTypes.OrderBy(t => t.Name)
            .Select(t => new SelectListItem(t.Name, t.Id.ToString())).ToListAsync();
        vm.FieldTypes = Enum.GetValues<CustomFieldType>()
            .Select(t => new SelectListItem(FieldTypeName(t), ((byte)t).ToString())).ToList();
        return vm;
    }

    private static string FieldTypeName(CustomFieldType t) => t switch
    {
        CustomFieldType.Metin => "Metin",
        CustomFieldType.Sayi => "Sayı",
        CustomFieldType.Tarih => "Tarih",
        CustomFieldType.Secim => "Seçim (tekli)",
        CustomFieldType.CokluSecim => "Seçim (çoklu)",
        CustomFieldType.EvetHayir => "Evet / Hayır",
        CustomFieldType.Kullanici => "Kullanıcı",
        _ => t.ToString()
    };
}
