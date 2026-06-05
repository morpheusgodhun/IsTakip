using IsTakip.Domain.Common;

namespace IsTakip.Domain.Entities;

/// <summary>Multi-tenant kiracı (şirket). Tenant bağımsız kayıt.</summary>
public class Tenant : BaseEntity, ISoftDeletable
{
    public string Name { get; set; } = default!;
    public string Code { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}

public class SystemSetting : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }
    public string Key { get; set; } = default!;
    public string? Value { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}
