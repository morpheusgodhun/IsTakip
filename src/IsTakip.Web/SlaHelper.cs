namespace IsTakip.Web;

/// <summary>
/// SLA göstergesi: son tarih ve tamamlanma durumuna göre metin + renk üretir.
/// Ayrı bir tablo gerektirmez; mevcut DueDate / tamamlanma bilgisinden hesaplanır.
/// </summary>
public static class SlaHelper
{
    public static (string Text, string Color) Status(DateOnly? due, bool done)
    {
        if (done) return ("Tamamlandı", "#22A06B");
        if (due is null) return ("—", "#5E6C84");

        var today = DateOnly.FromDateTime(DateTime.Today);
        var d = due.Value;
        if (d < today) return ($"Gecikti ({(today.DayNumber - d.DayNumber)} gün)", "#C9372C");
        if (d.DayNumber - today.DayNumber <= 2) return ("Süre yaklaşıyor", "#E2B203");
        return ("Zamanında", "#1F845A");
    }

    public static string SizeText(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:0.#} KB";
        return $"{bytes / (1024.0 * 1024.0):0.#} MB";
    }
}
