using System.Text.Json;
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
public class AutomationController : Controller
{
    private const string ManagePermission = "Automation.Manage";

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AutomationController(AppDbContext db, ICurrentUserService currentUser)
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
        var rules = await _db.AutomationRules.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new AutomationRuleListItemVM
            {
                Id = r.Id, Name = r.Name, TriggerName = TriggerName(r.TriggerEvent), IsActive = r.IsActive,
                ConditionCount = r.Conditions.Count, ActionCount = r.Actions.Count
            }).ToListAsync();
        return View(rules);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (Guard() is { } g) return g;
        var vm = new AutomationRuleEditVM
        {
            Conditions = Enumerable.Range(0, 5).Select(_ => new AutoConditionRow()).ToList(),
            Actions = Enumerable.Range(0, 5).Select(_ => new AutoActionRow()).ToList()
        };
        return View("Edit", await FillAsync(vm));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(long id)
    {
        if (Guard() is { } g) return g;
        var rule = await _db.AutomationRules
            .Include(r => r.Conditions).Include(r => r.Actions)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (rule is null) return NotFound();

        var vm = new AutomationRuleEditVM
        {
            Id = rule.Id, Name = rule.Name, TriggerEvent = rule.TriggerEvent, IsActive = rule.IsActive,
            Conditions = rule.Conditions.Select(c => new AutoConditionRow { FieldKey = c.FieldKey, Operator = c.Operator, Value = c.Value }).ToList(),
            Actions = rule.Actions.Select(a => ToRow(a)).ToList()
        };
        while (vm.Conditions.Count < 5) vm.Conditions.Add(new AutoConditionRow());
        while (vm.Actions.Count < 5) vm.Actions.Add(new AutoActionRow());
        return View(await FillAsync(vm));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AutomationRuleEditVM model)
    {
        if (Guard() is { } g) return g;
        if (string.IsNullOrWhiteSpace(model.Name)) ModelState.AddModelError(nameof(model.Name), "Kural adı zorunludur.");
        if (!ModelState.IsValid) return View(await FillAsync(model));

        AutomationRule rule;
        if (model.Id == 0) { rule = new AutomationRule { TenantId = TenantId }; _db.AutomationRules.Add(rule); }
        else rule = await _db.AutomationRules.Include(r => r.Conditions).Include(r => r.Actions).FirstAsync(r => r.Id == model.Id);

        rule.Name = model.Name.Trim();
        rule.TriggerEvent = model.TriggerEvent;
        rule.IsActive = model.IsActive;
        await _db.SaveChangesAsync();

        // Koşul ve aksiyonları yeniden yaz.
        _db.AutomationConditions.RemoveRange(_db.AutomationConditions.Where(c => c.AutomationRuleId == rule.Id));
        _db.AutomationActions.RemoveRange(_db.AutomationActions.Where(a => a.AutomationRuleId == rule.Id));
        await _db.SaveChangesAsync();

        foreach (var c in model.Conditions.Where(x => !string.IsNullOrWhiteSpace(x.FieldKey)))
            _db.AutomationConditions.Add(new AutomationCondition
            {
                AutomationRuleId = rule.Id, FieldKey = c.FieldKey!, Operator = c.Operator ?? "eq", Value = c.Value
            });

        foreach (var a in model.Actions.Where(x => x.ActionType != 0))
            _db.AutomationActions.Add(new AutomationAction
            {
                AutomationRuleId = rule.Id,
                ActionType = (AutomationActionType)a.ActionType,
                ParametersJson = BuildParams((AutomationActionType)a.ActionType, a)
            });

        await _db.SaveChangesAsync();
        TempData["Success"] = "Otomasyon kuralı kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        if (Guard() is { } g) return g;
        var rule = await _db.AutomationRules.FirstOrDefaultAsync(r => r.Id == id);
        if (rule is not null) { _db.AutomationRules.Remove(rule); await _db.SaveChangesAsync(); TempData["Success"] = "Kural silindi."; }
        return RedirectToAction(nameof(Index));
    }

    private static string? BuildParams(AutomationActionType type, AutoActionRow row)
    {
        object? obj = type switch
        {
            AutomationActionType.Ata => new { userId = row.TargetUserId },
            AutomationActionType.BildirimGonder => new { userId = row.TargetUserId, message = row.Value },
            AutomationActionType.DurumDegistir => new { stateId = row.Value },
            AutomationActionType.EtiketEkle => new { labelId = row.Value },
            _ => null
        };
        return obj is null ? null : JsonSerializer.Serialize(obj);
    }

    private static AutoActionRow ToRow(AutomationAction a)
    {
        var row = new AutoActionRow { ActionType = (byte)a.ActionType };
        try
        {
            if (!string.IsNullOrWhiteSpace(a.ParametersJson))
            {
                using var doc = JsonDocument.Parse(a.ParametersJson);
                var root = doc.RootElement;
                if (root.TryGetProperty("userId", out var u) && u.ValueKind != JsonValueKind.Null)
                    row.TargetUserId = u.ValueKind == JsonValueKind.Number ? u.GetInt64() : (long.TryParse(u.GetString(), out var lv) ? lv : null);
                if (root.TryGetProperty("message", out var m)) row.Value = m.GetString();
                if (root.TryGetProperty("stateId", out var s)) row.Value = s.ValueKind == JsonValueKind.Number ? s.GetInt64().ToString() : s.GetString();
                if (root.TryGetProperty("labelId", out var l)) row.Value = l.ValueKind == JsonValueKind.Number ? l.GetInt64().ToString() : l.GetString();
            }
        }
        catch { }
        return row;
    }

    private async Task<AutomationRuleEditVM> FillAsync(AutomationRuleEditVM vm)
    {
        vm.Triggers = Enum.GetValues<AutomationTrigger>()
            .Select(t => new SelectListItem(TriggerName(t), ((byte)t).ToString())).ToList();
        vm.ActionTypes = new List<SelectListItem>
        {
            new("— Yok —", "0"),
            new("Kullanıcıya Ata", ((byte)AutomationActionType.Ata).ToString()),
            new("Bildirim Gönder", ((byte)AutomationActionType.BildirimGonder).ToString()),
            new("Durum Değiştir", ((byte)AutomationActionType.DurumDegistir).ToString()),
            new("Etiket Ekle", ((byte)AutomationActionType.EtiketEkle).ToString())
        };
        vm.FieldKeys = new List<SelectListItem>
        {
            new("— Seçin —", ""),
            new("Öncelik (Id)", "Priority"),
            new("Tür (Id)", "Type"),
            new("Departman (Id)", "Department"),
            new("Durum (Id)", "State")
        };
        vm.Operators = new List<SelectListItem> { new("eşittir", "eq"), new("eşit değildir", "ne") };
        vm.Users = await _db.Users.Where(u => u.TenantId == TenantId && u.Status == UserStatus.Aktif)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem(u.FirstName + " " + u.LastName, u.Id.ToString())).ToListAsync();
        return vm;
    }

    private static string TriggerName(AutomationTrigger t) => t switch
    {
        AutomationTrigger.Olusturuldu => "Görev oluşturulduğunda",
        AutomationTrigger.DurumGuncellendi => "Durum değiştiğinde",
        AutomationTrigger.Atama => "Atama yapıldığında",
        AutomationTrigger.Yorum => "Yorum eklendiğinde",
        AutomationTrigger.SonTarihYaklasti => "Son tarih yaklaştığında",
        _ => t.ToString()
    };
}
