using IsTakip.Domain.Common;

namespace IsTakip.Application.Common;

/// <summary>
/// Bildirim üretimi soyutlaması. Veritabanına bildirim yazar ve (SignalR ile) anlık iletir.
/// Uygulama/altyapı katmanı bu arayüze bağlıdır; gerçek implementasyon Web katmanındadır.
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(long recipientUserId, string title, string? body, string? link,
        NotificationType type, CancellationToken ct = default);
}
