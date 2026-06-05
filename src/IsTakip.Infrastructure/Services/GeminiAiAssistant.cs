using System.Text;
using System.Text.Json;
using IsTakip.Application.Common;
using Microsoft.Extensions.Configuration;

namespace IsTakip.Infrastructure.Services;

/// <summary>
/// Google Gemini (generativelanguage API) tabanlı AI asistanı.
/// Tamamen doğrulanmış aktif model listesine göre v1beta endpoint'ini kullanır.
/// </summary>
public class GeminiAiAssistant : IAiAssistant
{
    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string _model;

    public GeminiAiAssistant(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Gemini:ApiKey"];
        var model = config["Gemini:Model"];
        // Eğer appsettings boş kalırsa listedeki en kararlı modeli varsayılan atıyoruz
        _model = string.IsNullOrWhiteSpace(model) ? "gemini-3.5-flash" : model;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_apiKey);

    public async Task<string> AskAsync(string prompt, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return "AI servisi henüz yapılandırılmamış. appsettings.json içindeki \"Gemini:ApiKey\" alanını doldurun.";

        if (string.IsNullOrWhiteSpace(prompt))
            return "Lütfen bir soru veya istek yazın.";

        // DOĞRULANMIŞ URL: Hesabının desteklediği v1beta kapısı ve dinamik model rotası
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

        var payload = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } }
        };

        // Küçük harf (camelCase) kuralını garanti altına alıyoruz
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var jsonString = JsonSerializer.Serialize(payload, jsonOptions);

        using var content = new StringContent(jsonString, Encoding.UTF8, "application/json");

        try
        {
            using var resp = await _http.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            if (!resp.IsSuccessStatusCode)
                return $"AI servisi hata döndürdü ({(int)resp.StatusCode}). Detay: {body}";

            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return string.IsNullOrWhiteSpace(text) ? "Yanıt alınamadı." : text;
        }
        catch (Exception ex)
        {
            return "AI servisine ulaşılamadı: " + ex.Message;
        }
    }
}
