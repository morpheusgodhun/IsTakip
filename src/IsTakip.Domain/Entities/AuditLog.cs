namespace IsTakip.Domain.Entities;

/// <summary>Denetim logu: kim, ne, ne zaman, eski/yeni değer. FK taşımaz (yazma performansı).</summary>
public class AuditLog
{
    public long Id { get; set; }
    public long? TenantId { get; set; }
    public long? UserId { get; set; }
    public string Action { get; set; } = default!;
    public string EntityName { get; set; } = default!;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
