using IsTakip.Domain.Common;

namespace IsTakip.Domain.Entities;

public class WorkItemType : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? IconName { get; set; }
    public string? ColorHex { get; set; }
    public long? DefaultWorkflowId { get; set; }
    public string? KeyPrefix { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public Workflow? DefaultWorkflow { get; set; }
}

public class Project : AuditableTenantEntity
{
    public string Name { get; set; } = default!;
    public string Key { get; set; } = default!;
    public string? Description { get; set; }
    public long? DepartmentId { get; set; }
    public long? LeadUserId { get; set; }
    public decimal? Budget { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Aktif;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public Department? Department { get; set; }
    public AppUser? Lead { get; set; }
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}

public class ProjectMember
{
    public long ProjectId { get; set; }
    public long UserId { get; set; }
    public bool IsManager { get; set; }
    public Project Project { get; set; } = default!;
    public AppUser User { get; set; } = default!;
}

public class WorkPeriod : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public long? ProjectId { get; set; }
    public string Name { get; set; } = default!;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public WorkPeriodStatus Status { get; set; } = WorkPeriodStatus.Planlandi;
    public string? Goal { get; set; }
    public bool IsDeleted { get; set; }
    public Project? Project { get; set; }
}

/// <summary>Ana iş kaydı: görev, talep, hata vb. Tür ile ayrışır, dinamik iş akışıyla yönetilir.</summary>
public class WorkItem : AuditableTenantEntity
{
    public string Key { get; set; } = default!;
    public long WorkItemTypeId { get; set; }
    public long? ProjectId { get; set; }
    public long? WorkPeriodId { get; set; }
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public long? PriorityId { get; set; }
    public long WorkflowId { get; set; }
    public long CurrentStateId { get; set; }
    public long? AssigneeUserId { get; set; }
    public long? ReporterUserId { get; set; }
    public long? DepartmentId { get; set; }
    public long? ParentWorkItemId { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int? EstimatedMinutes { get; set; }
    /// <summary>Optimistic concurrency (kanban sürükle-bırak çakışmaları).</summary>
    public byte[] RowVersion { get; set; } = default!;

    public WorkItemType Type { get; set; } = default!;
    public Project? Project { get; set; }
    public Priority? Priority { get; set; }
    public Workflow Workflow { get; set; } = default!;
    public WorkflowState CurrentState { get; set; } = default!;
    public AppUser? Assignee { get; set; }
    public AppUser? Reporter { get; set; }
    public Department? Department { get; set; }
    public WorkItem? Parent { get; set; }
    public ICollection<WorkItem> Children { get; set; } = new List<WorkItem>();
    public ICollection<Comment> Comments { get; set; } = new List<Comment>();
    public ICollection<WorkItemLabel> Labels { get; set; } = new List<WorkItemLabel>();
}

public class Label : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? ColorHex { get; set; }
}

public class WorkItemLabel
{
    public long WorkItemId { get; set; }
    public long LabelId { get; set; }
    public WorkItem WorkItem { get; set; } = default!;
    public Label Label { get; set; } = default!;
}

public class WorkItemWatcher
{
    public long WorkItemId { get; set; }
    public long UserId { get; set; }
}

public class WorkItemStateHistory : BaseEntity
{
    public long WorkItemId { get; set; }
    public long? FromStateId { get; set; }
    public long ToStateId { get; set; }
    public long? ChangedByUserId { get; set; }
    public DateTime ChangedAtUtc { get; set; }
}
