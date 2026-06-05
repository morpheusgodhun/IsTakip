using IsTakip.Domain.Common;

namespace IsTakip.Domain.Entities;

public class Priority : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? ColorHex { get; set; }
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
}

public class Workflow : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<WorkflowState> States { get; set; } = new List<WorkflowState>();
    public ICollection<WorkflowTransition> Transitions { get; set; } = new List<WorkflowTransition>();
}

public class WorkflowState : BaseEntity
{
    public long WorkflowId { get; set; }
    public string Name { get; set; } = default!;
    public StateCategory Category { get; set; } = StateCategory.Yapilacak;
    public string? ColorHex { get; set; }
    public int SortOrder { get; set; }
    public bool IsInitial { get; set; }
    public bool IsFinal { get; set; }
    public Workflow Workflow { get; set; } = default!;
}

public class WorkflowTransition : BaseEntity
{
    public long WorkflowId { get; set; }
    public long FromStateId { get; set; }
    public long ToStateId { get; set; }
    public string? Name { get; set; }
    public string? RequiredPermissionKey { get; set; }
    public Workflow Workflow { get; set; } = default!;
    public WorkflowState FromState { get; set; } = default!;
    public WorkflowState ToState { get; set; } = default!;
}
