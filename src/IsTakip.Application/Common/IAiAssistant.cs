namespace IsTakip.Application.Common;

/// <summary>
/// Yapay zeka asistanı soyutlaması. Sağlayıcı (Gemini vb.) Infrastructure'da uygulanır.
/// API anahtarı yapılandırmadan (appsettings / ortam değişkeni) okunur; koda gömülmez.
/// </summary>
public interface IAiAssistant
{
    bool IsConfigured { get; }
    Task<string> AskAsync(string prompt, CancellationToken ct = default);
}
