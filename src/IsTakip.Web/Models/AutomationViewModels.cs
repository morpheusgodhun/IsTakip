using System.ComponentModel.DataAnnotations;
using IsTakip.Domain.Common;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IsTakip.Web.Models;

public class AutomationRuleListItemVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string TriggerName { get; set; } = default!;
    public bool IsActive { get; set; }
    public int ConditionCount { get; set; }
    public int ActionCount { get; set; }
}

public class AutoConditionRow
{
    public string? FieldKey { get; set; }
    public string? Operator { get; set; }
    public string? Value { get; set; }
}

public class AutoActionRow
{
    public byte ActionType { get; set; }     // 0 = yok
    public long? TargetUserId { get; set; }
    public string? Value { get; set; }
}

public class AutomationRuleEditVM
{
    public long Id { get; set; }

    [Required(ErrorMessage = "Kural adı zorunludur.")]
    [Display(Name = "Kural Adı")]
    public string Name { get; set; } = default!;

    [Display(Name = "Tetikleyici")]
    public AutomationTrigger TriggerEvent { get; set; } = AutomationTrigger.Olusturuldu;

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;

    public List<AutoConditionRow> Conditions { get; set; } = new();
    public List<AutoActionRow> Actions { get; set; } = new();

    public List<SelectListItem> Triggers { get; set; } = new();
    public List<SelectListItem> FieldKeys { get; set; } = new();
    public List<SelectListItem> Operators { get; set; } = new();
    public List<SelectListItem> ActionTypes { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}
