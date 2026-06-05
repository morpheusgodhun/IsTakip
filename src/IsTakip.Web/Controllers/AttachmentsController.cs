using IsTakip.Application.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class AttachmentsController : Controller
{
    private const string ManagerPermission = "WorkItem.Delete";

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IWebHostEnvironment _env;

    public AttachmentsController(AppDbContext db, ICurrentUserService currentUser, IWebHostEnvironment env)
    {
        _db = db;
        _currentUser = currentUser;
        _env = env;
    }

    private string StorageDir => Path.Combine(_env.ContentRootPath, "App_Data", "attachments");

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(52_428_800)] // 50 MB
    public async Task<IActionResult> Upload(long workItemId, IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Lütfen bir dosya seçin.";
            return RedirectToAction("Details", "WorkItems", new { id = workItemId });
        }

        var tenantId = _currentUser.TenantId ?? throw new InvalidOperationException("Aktif kiracı yok.");
        var exists = await _db.WorkItems.AnyAsync(w => w.Id == workItemId);
        if (!exists) return NotFound();

        Directory.CreateDirectory(StorageDir);
        var ext = Path.GetExtension(file.FileName);
        var storedName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(StorageDir, storedName);

        await using (var stream = System.IO.File.Create(fullPath))
            await file.CopyToAsync(stream);

        _db.Attachments.Add(new Attachment
        {
            TenantId = tenantId,
            WorkItemId = workItemId,
            FileName = Path.GetFileName(file.FileName),
            ContentType = file.ContentType,
            SizeBytes = file.Length,
            StoragePath = storedName,
            Version = 1,
            UploadedByUserId = _currentUser.UserId,
            UploadedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = "Dosya yüklendi.";
        return RedirectToAction("Details", "WorkItems", new { id = workItemId });
    }

    [HttpGet]
    public async Task<IActionResult> Download(long id)
    {
        var att = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == id);
        if (att is null) return NotFound();

        var fullPath = Path.Combine(StorageDir, att.StoragePath);
        if (!System.IO.File.Exists(fullPath)) return NotFound();

        var stream = System.IO.File.OpenRead(fullPath);
        return File(stream, att.ContentType ?? "application/octet-stream", att.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(long id)
    {
        var att = await _db.Attachments.FirstOrDefaultAsync(a => a.Id == id);
        if (att is null) return NotFound();
        if (!_currentUser.HasPermission(ManagerPermission))
            return RedirectToAction("AccessDenied", "Account");

        var workItemId = att.WorkItemId;
        var fullPath = Path.Combine(StorageDir, att.StoragePath);
        try { if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath); } catch { /* dosya yoksa yoksay */ }

        _db.Attachments.Remove(att); // interceptor soft-delete uygular
        await _db.SaveChangesAsync();

        TempData["Success"] = "Dosya silindi.";
        return RedirectToAction("Details", "WorkItems", new { id = workItemId });
    }
}
