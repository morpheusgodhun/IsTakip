using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IsTakip.Infrastructure.Services;

/// <summary>
/// SQL seed scripti veriyi (kiracı, roller, izinler, kullanıcı kaydı) kurar; bu seeder
/// yalnızca uygulama katmanına ait olan kısmı tamamlar: admin kullanıcısının parolasını
/// ASP.NET Core Identity hash'i ile atar.
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

        if (!string.IsNullOrEmpty(admin.PasswordHash))
        {
            _logger.LogInformation("Admin parolası zaten atanmış, atlanıyor.");
            return;
        }

        admin.PasswordHash = _userManager.PasswordHasher.HashPassword(admin, DefaultAdminPassword);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Admin parolası atandı. Kullanıcı: {User}", DefaultAdminUserName);
    }
}
