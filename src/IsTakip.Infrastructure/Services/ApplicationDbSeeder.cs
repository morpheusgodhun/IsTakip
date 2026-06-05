using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IsTakip.Infrastructure.Services;

/// <summary>
/// SQL seed scripti veriyi (kiracı, roller, izinler, kullanıcı kaydı) kurar; bu seeder
/// uygulama katmanına ait kısmı tamamlar: admin parolasını ASP.NET Core Identity hash'i
/// ile atar ve İK/Satın Alma talep türlerini (görev türü + form alanları) idempotent kurar.
/// </summary>
public class ApplicationDbSeeder
{
    private const string DefaultAdminUserName = "admin";
    private const string DefaultAdminPassword = "Admin!2345"; // İlk girişten sonra değiştirilmelidir.

    private readonly AppDbContext _db;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<ApplicationDbSeeder> _logger;

    public ApplicationDbSeeder(AppDbContext db, UserManager<AppUser> userManager, ILogger<ApplicationDbSeeder> logger)
    {
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var admin = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.NormalizedUserName == DefaultAdminUserName.ToUpperInvariant());

        if (admin is null)
        {
            _logger.LogWarning("Admin kullanıcısı bulunamadı. Önce 02_Seed.sql çalıştırılmalı.");
            return;
        }

        if (string.IsNullOrEmpty(admin.PasswordHash))
        {
            admin.PasswordHash = _userManager.PasswordHasher.HashPassword(admin, DefaultAdminPassword);
            await _db.SaveChangesAsync();
            _logger.LogInformation("Admin parolası atandı. Kullanıcı: {User}", DefaultAdminUserName);
        }

        await SeedRequestModulesAsync(admin.TenantId);
    }

    // ----- İK / Satın Alma talep türleri (Form Tasarımcısı + Onay Motoru üstünde) -----
    private async Task SeedRequestModulesAsync(long tenantId)
    {
        var workflowId = await _db.Workflows.IgnoreQueryFilters()
            .Where(w => w.TenantId == tenantId && w.IsActive)
            .Select(w => (long?)w.Id).FirstOrDefaultAsync();

        await EnsureTypeAsync(tenantId, workflowId, "İzin Talebi", "IZN", "#6554C0", new[]
        {
            Field("İzin Türü", CustomFieldType.Secim, true, "Yıllık İzin", "Mazeret İzni", "Ücretsiz İzin", "Hastalık İzni"),
            Field("Başlangıç Tarihi", CustomFieldType.Tarih, true),
            Field("Bitiş Tarihi", CustomFieldType.Tarih, true),
            Field("Gün Sayısı", CustomFieldType.Sayi, false),
            Field("Açıklama", CustomFieldType.Metin, false)
        });

        await EnsureTypeAsync(tenantId, workflowId, "Avans Talebi", "AVN", "#0C66E4", new[]
        {
            Field("Tutar", CustomFieldType.Sayi, true),
            Field("Para Birimi", CustomFieldType.Secim, true, "TL", "USD", "EUR"),
            Field("Gerekçe", CustomFieldType.Metin, true)
        });

        await EnsureTypeAsync(tenantId, workflowId, "Fazla Mesai Talebi", "FZM", "#E2B203", new[]
        {
            Field("Mesai Tarihi", CustomFieldType.Tarih, true),
            Field("Saat", CustomFieldType.Sayi, true),
            Field("Açıklama", CustomFieldType.Metin, false)
        });

        await EnsureTypeAsync(tenantId, workflowId, "Satın Alma Talebi", "SAT", "#22A06B", new[]
        {
            Field("Ürün / Hizmet", CustomFieldType.Metin, true),
            Field("Miktar", CustomFieldType.Sayi, true),
            Field("Tahmini Tutar", CustomFieldType.Sayi, false),
            Field("Tedarikçi", CustomFieldType.Metin, false),
            Field("Aciliyet", CustomFieldType.Secim, false, "Normal", "Acil")
        });

        await _db.SaveChangesAsync();
    }

    private static (string Name, CustomFieldType Type, bool Required, string[] Options) Field(
        string name, CustomFieldType type, bool required, params string[] options)
        => (name, type, required, options);

    private async Task EnsureTypeAsync(long tenantId, long? workflowId, string name, string prefix, string color,
        (string Name, CustomFieldType Type, bool Required, string[] Options)[] fields)
    {
        var exists = await _db.WorkItemTypes.IgnoreQueryFilters()
            .AnyAsync(t => t.TenantId == tenantId && t.Name == name);
        if (exists) return;

        var type = new WorkItemType
        {
            TenantId = tenantId,
            Name = name,
            KeyPrefix = prefix,
            ColorHex = color,
            DefaultWorkflowId = workflowId,
            IsActive = true
        };
        _db.WorkItemTypes.Add(type);
        await _db.SaveChangesAsync();

        int sort = 0;
        foreach (var f in fields)
        {
            var def = new CustomFieldDefinition
            {
                TenantId = tenantId,
                WorkItemTypeId = type.Id,
                Name = f.Name,
                FieldType = f.Type,
                IsRequired = f.Required,
                SortOrder = sort++
            };
            _db.CustomFieldDefinitions.Add(def);
            await _db.SaveChangesAsync();

            if ((f.Type == CustomFieldType.Secim || f.Type == CustomFieldType.CokluSecim) && f.Options.Length > 0)
            {
                int oi = 0;
                foreach (var opt in f.Options)
                    _db.CustomFieldOptions.Add(new CustomFieldOption { CustomFieldDefinitionId = def.Id, Value = opt, SortOrder = oi++ });
                await _db.SaveChangesAsync();
            }
        }

        _logger.LogInformation("Talep türü tohumlandı: {Name}", name);
    }
}
