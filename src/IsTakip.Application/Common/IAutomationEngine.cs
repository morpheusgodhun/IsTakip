using IsTakip.Domain.Common;

namespace IsTakip.Application.Common;

/// <summary>
/// Olay tetiklendiğinde (görev oluşturma, durum değişimi vb.) aktif otomasyon
/// kurallarını değerlendirir ve eşleşen kuralların aksiyonlarını çalıştırır.
/// </summary>
public interface IAutomationEngine
{
    Task RunAsync(AutomationTrigger trigger, long workItemId, CancellationToken ct = default);
}
