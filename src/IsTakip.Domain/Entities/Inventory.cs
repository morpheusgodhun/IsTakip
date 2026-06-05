using IsTakip.Domain.Common;

namespace IsTakip.Domain.Entities;

/// <summary>Envanter türü/kategorisi (Laptop, Telefon, Araç, Mobilya vb.).</summary>
public class InventoryCategory : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsDeleted { get; set; }
}

/// <summary>Envanter kalemi (demirbaş). Zimmet durumu ve mevcut taşıyıcı burada tutulur.</summary>
public class InventoryItem : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public long? CategoryId { get; set; }
    public string? SerialNo { get; set; }
    public string? Code { get; set; }                  // Demirbaş / zimmet no
    public InventoryStatus Status { get; set; } = InventoryStatus.Depoda;
    public long? CurrentHolderUserId { get; set; }      // Şu an kimde (teslim edilen kişi)
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>Zimmet (teslim/iade) hareketi. Kalıcı geçmiş; soft-delete yok.</summary>
public class InventoryAssignment : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }
    public long InventoryItemId { get; set; }
    public long AssignedToUserId { get; set; }          // Teslim edilen kişi
    public long? AssignedByUserId { get; set; }         // Teslim eden kişi
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }        // null => hâlâ zimmette
    public long? ReturnedByUserId { get; set; }         // İade alan
    public string? Notes { get; set; }
}

/// <summary>Sayım oturumu.</summary>
public class InventoryCount : BaseEntity, ITenantEntity
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public long? CountedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Sayım satırı: bir kalemin sayımda bulunup bulunmadığı.</summary>
public class InventoryCountLine : BaseEntity
{
    public long InventoryCountId { get; set; }
    public long InventoryItemId { get; set; }
    public bool IsFound { get; set; }
    public string? Note { get; set; }
}
