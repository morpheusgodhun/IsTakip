using System.Text.Json;
using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Infrastructure.Services;

public class AutomationEngine : IAutomationEngine
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notify;

    public AutomationEngine(AppDbContext db, INotificationService notify)
    {
        _db = db;
        _notify = notify;
    }

    public async Task RunAsync(AutomationTrigger trigger, long workItemId, CancellationToken ct = default)
    {
        var item = await _db.WorkItems.FirstOrDefaultAsync(w => w.Id == workItemId, ct);
        if (item is null) return;

        var rules = await _db.AutomationRules
            .Where(r => r.IsActive && r.TriggerEvent == trigger)
            .Include(r => r.Conditions)
            .Include(r => r.Actions)
            .ToListAsync(ct);
        if (rules.Count == 0) return;

        bool changed = false;
        foreach (var rule in rules)
        {
            if (!ConditionsMatch(rule.Conditions, item)) continue;
            foreach (var action in rule.Actions)
                changed |= await ExecuteAsync(action, item, ct);
        }

        if (changed) await _db.SaveChangesAsync(ct);
    }

    private static bool ConditionsMatch(IEnumerable<AutomationCondition> conditions, WorkItem item)
    {
        foreach (var c in conditions)
        {
            var actual = c.FieldKey switch
            {
                "Priority" => item.PriorityId?.ToString(),
                "Type" => item.WorkItemTypeId.ToString(),
                "Department" => item.DepartmentId?.ToString(),
                "State" => item.CurrentStateId.ToString(),
                _ => null
            };
            var expected = c.Value;
            bool eq = string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            bool ok = c.Operator == "ne" ? !eq : eq;
            if (!ok) return false;
        }
        return true;
    }

    private async Task<bool> ExecuteAsync(AutomationAction action, WorkItem item, CancellationToken ct)
    {
        var p = Parse(action.ParametersJson);
        switch (action.ActionType)
        {
            case AutomationActionType.Ata:
                if (TryLong(p, "userId", out var assignee))
                {
                    item.AssigneeUserId = assignee;
                    await _notify.NotifyAsync(assignee, "Size yeni bir iş atandı (otomasyon)",
                        $"{item.Key} · {item.Title}", $"/WorkItems/Details/{item.Id}", NotificationType.Atama, ct);
                    return true;
                }
                return false;

            case AutomationActionType.BildirimGonder:
                long? target = TryLong(p, "userId", out var uid) ? uid : item.AssigneeUserId;
                if (target is { } t)
                {
                    var msg = GetString(p, "message") ?? $"{item.Key} · {item.Title}";
                    await _notify.NotifyAsync(t, "Otomasyon bildirimi", msg,
                        $"/WorkItems/Details/{item.Id}", NotificationType.Sistem, ct);
                }
                return false; // bildirim WorkItem'ı değiştirmez

            case AutomationActionType.DurumDegistir:
                if (TryLong(p, "stateId", out var stateId))
                {
                    var valid = await _db.WorkflowStates
                        .AnyAsync(s => s.Id == stateId && s.WorkflowId == item.WorkflowId, ct);
                    if (valid && item.CurrentStateId != stateId)
                    {
                        item.CurrentStateId = stateId;
                        _db.WorkItemStateHistory.Add(new WorkItemStateHistory
                        {
                            WorkItemId = item.Id,
                            ToStateId = stateId,
                            ChangedByUserId = null,
                            ChangedAtUtc = DateTime.UtcNow
                        });
                        return true;
                    }
                }
                return false;

            case AutomationActionType.EtiketEkle:
                if (TryLong(p, "labelId", out var labelId))
                {
                    var exists = await _db.WorkItemLabels.AnyAsync(x => x.WorkItemId == item.Id && x.LabelId == labelId, ct);
                    if (!exists)
                    {
                        _db.WorkItemLabels.Add(new WorkItemLabel { WorkItemId = item.Id, LabelId = labelId });
                        return true;
                    }
                }
                return false;

            default: // EpostaGonder vb. — e-posta altyapısı yok, atla
                return false;
        }
    }

    private static Dictionary<string, JsonElement> Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone());
        }
        catch { return new(); }
    }

    private static bool TryLong(Dictionary<string, JsonElement> p, string key, out long value)
    {
        value = 0;
        if (!p.TryGetValue(key, out var el)) return false;
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt64(out value)) return true;
        if (el.ValueKind == JsonValueKind.String && long.TryParse(el.GetString(), out value)) return true;
        return false;
    }

    private static string? GetString(Dictionary<string, JsonElement> p, string key) =>
        p.TryGetValue(key, out var el) ? (el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString()) : null;
}
