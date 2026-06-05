using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace IsTakip.Web.Services;

/// <summary>
/// Bildirimi veritabanına yazar ve alıcı çevrimiçiyse SignalR ile anında iletir.
/// INotificationService Application katmanında tanımlı; bu implementasyon Web'de yaşar
/// çünkü hem AppDbContext'e hem de SignalR hub bağlamına ihtiyaç duyar.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _db;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly ICurrentUserService _currentUser;

    public NotificationService(AppDbContext db, IHubContext<NotificationHub> hub, ICurrentUserService currentUser)
    {
        _db = db;
        _hub = hub;
        _currentUser = currentUser;
    }

    public async Task NotifyAsync(long recipientUserId, string title, string? body, string? link,
        NotificationType type, CancellationToken ct = default)
    {
        // Alıcı, gönderenle aynı kiracıdadır; aktif kiracı yoksa atla.
        var tenantId = _currentUser.TenantId;
        if (tenantId is null) return;

        var notification = new Notification
        {
            TenantId = tenantId.Value,
            RecipientUserId = recipientUserId,
            Title = title,
            Body = body,
            LinkUrl = link,
            Type = type,
            IsRead = false,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.Notifications.Add(notification);
        await _db.SaveChangesAsync(ct);

        await _hub.Clients.User(recipientUserId.ToString()).SendAsync("notify", new
        {
            id = notification.Id,
            title = notification.Title,
            body = notification.Body,
            link = notification.LinkUrl,
            createdAt = notification.CreatedAtUtc
        }, ct);
    }
}
