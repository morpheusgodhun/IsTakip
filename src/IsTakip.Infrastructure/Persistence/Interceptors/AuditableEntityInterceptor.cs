using System.Text.Json;
using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IsTakip.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Kaydetme anında üç işi merkezi olarak yapar:
///  - IAuditable kolonlarını (oluşturma/güncelleme zamanı + kullanıcı) doldurur,
///  - ISoftDeletable kayıtlardaki silme işlemini IsDeleted = true güncellemesine çevirir,
///  - değişiklikleri AuditLogs tablosuna (kim/ne/ne zaman/eski/yeni) yazar.
/// </summary>
public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUser;

    public AuditableEntityInterceptor(ICurrentUserService currentUser) => _currentUser = currentUser;

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Apply(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;
        var userId = _currentUser.UserId;
        var tenantId = _currentUser.TenantId;

        // Audit log eklemeden önce mevcut girişleri kopyala (koleksiyon değişimini önlemek için).
        var entries = context.ChangeTracker.Entries().ToList();
        var auditLogs = new List<AuditLog>();

        foreach (var entry in entries)
        {
            if (entry.Entity is AuditLog) continue;

            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    auditable.CreatedAtUtc = now;
                    auditable.CreatedByUserId ??= userId;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditable.UpdatedAtUtc = now;
                    auditable.UpdatedByUserId = userId;
                }
            }

            // Soft delete: fiziksel silmeyi mantıksal silmeye çevir.
            if (entry.State == EntityState.Deleted && entry.Entity is ISoftDeletable softDeletable)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
            }

            var log = BuildAuditLog(entry, userId, tenantId, now);
            if (log is not null) auditLogs.Add(log);
        }

        if (auditLogs.Count > 0)
            context.Set<AuditLog>().AddRange(auditLogs);
    }

    private static AuditLog? BuildAuditLog(EntityEntry entry, long? userId, long? tenantId, DateTime now)
    {
        if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            return null;

        var action = entry.State switch
        {
            EntityState.Added => "Insert",
            EntityState.Modified => "Update",
            EntityState.Deleted => "Delete",
            _ => "Unknown"
        };

        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsPrimaryKey()) continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[prop.Metadata.Name] = prop.CurrentValue;
                    break;
                case EntityState.Deleted:
                    oldValues[prop.Metadata.Name] = prop.OriginalValue;
                    break;
                case EntityState.Modified when prop.IsModified:
                    oldValues[prop.Metadata.Name] = prop.OriginalValue;
                    newValues[prop.Metadata.Name] = prop.CurrentValue;
                    break;
            }
        }

        var keyValue = entry.Metadata.FindPrimaryKey()?.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .FirstOrDefault();

        return new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = keyValue,
            OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
            CreatedAtUtc = now
        };
    }
}
