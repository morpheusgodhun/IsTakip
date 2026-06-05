namespace IsTakip.Domain.Common;

/// <summary>Tüm entity'ler için BIGINT kimlik taşıyan temel sınıf.</summary>
public abstract class BaseEntity
{
    public long Id { get; set; }
}

/// <summary>Kiracıya (tenant) bağlı entity'leri işaretler. Global query filter bu arayüze göre uygulanır.</summary>
public interface ITenantEntity
{
    long TenantId { get; set; }
}

/// <summary>Soft delete destekleyen entity'leri işaretler.</summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
}

/// <summary>Denetim kolonlarını taşıyan entity'leri işaretler. Interceptor bu kolonları otomatik doldurur.</summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; set; }
    long? CreatedByUserId { get; set; }
    DateTime? UpdatedAtUtc { get; set; }
    long? UpdatedByUserId { get; set; }
}

/// <summary>Kiracıya bağlı, soft delete ve denetim kolonlu yaygın taban.</summary>
public abstract class AuditableTenantEntity : BaseEntity, ITenantEntity, ISoftDeletable, IAuditable
{
    public long TenantId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public long? UpdatedByUserId { get; set; }
}
