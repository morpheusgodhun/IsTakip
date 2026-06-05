using IsTakip.Domain.Common;

namespace IsTakip.Domain.Entities;

public class TimeLog : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }
    public long WorkItemId { get; set; }
    public long UserId { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsOvertime { get; set; }
    public string? Description { get; set; }
    public DateOnly LogDate { get; set; }
}

public class ApprovalRequest : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }
    public long WorkItemId { get; set; }
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Beklemede;
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<ApprovalStep> Steps { get; set; } = new List<ApprovalStep>();
}

public class ApprovalStep : BaseEntity
{
    public long ApprovalRequestId { get; set; }
    public int StepOrder { get; set; }
    public long ApproverUserId { get; set; }
    public ApprovalStepStatus Status { get; set; } = ApprovalStepStatus.Beklemede;
    public string? Comment { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public ApprovalRequest ApprovalRequest { get; set; } = default!;
}

public class AutomationRule : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public AutomationTrigger TriggerEvent { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public ICollection<AutomationCondition> Conditions { get; set; } = new List<AutomationCondition>();
    public ICollection<AutomationAction> Actions { get; set; } = new List<AutomationAction>();
}

public class AutomationCondition : BaseEntity
{
    public long AutomationRuleId { get; set; }
    public string FieldKey { get; set; } = default!;
    public string Operator { get; set; } = default!;
    public string? Value { get; set; }
}

public class AutomationAction : BaseEntity
{
    public long AutomationRuleId { get; set; }
    public AutomationActionType ActionType { get; set; }
    public string? ParametersJson { get; set; }
}

public class Notification : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }
    public long RecipientUserId { get; set; }
    public string Title { get; set; } = default!;
    public string? Body { get; set; }
    public string? LinkUrl { get; set; }
    public NotificationType Type { get; set; } = NotificationType.Sistem;
    public bool IsRead { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class NotificationPreference : BaseEntity
{
    public long UserId { get; set; }
    public NotificationType NotificationType { get; set; }
    public bool InApp { get; set; } = true;
    public bool Email { get; set; } = true;
}
