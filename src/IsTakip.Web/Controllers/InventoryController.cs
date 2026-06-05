using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Controllers;

[Authorize]
public class InventoryController : Controller
{
    private const string ManagePermission = "Organization.Manage";

    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public InventoryController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private long TenantId => _currentUser.TenantId ?? 0;
    private IActionResult? Guard() =>
        _currentUser.HasPermission(ManagePermission) ? null : RedirectToAction("AccessDenied", "Account");

    private Task<Dictionary<long, string>> UserNamesAsync() =>
        _db.Users.Where(u => u.TenantId == TenantId)
            .ToDictionaryAsync(u => u.Id, u => u.FirstName + " " + u.LastName);

    private async Task<List<SelectListItem>> UserOptionsAsync() =>
        await _db.Users.Where(u => u.TenantId == TenantId && u.Status == UserStatus.Aktif)
            .OrderBy(u => u.FirstName)
            .Select(u => new SelectListItem(u.FirstName + " " + u.LastName, u.Id.ToString())).ToListAsync();

    private async Task<List<SelectListItem>> CategoryOptionsAsync() =>
        await _db.InventoryCategories.OrderBy(c => c.Name)
            .Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToListAsync();

    private static List<SelectListItem> StatusOptions() =>
        Enum.GetValues<InventoryStatus>().Select(s => new SelectListItem(StatusText(s), ((byte)s).ToString())).ToList();

    private static string StatusText(InventoryStatus s) => s switch
    {
        InventoryStatus.Depoda => "Depoda",
        InventoryStatus.Zimmetli => "Zimmetli",
        InventoryStatus.Bakimda => "Bakımda",
        InventoryStatus.Hurda => "Hurda",
        InventoryStatus.Kayip => "Kayıp",
        _ => s.ToString()
    };
    private static string StatusColor(InventoryStatus s) => s switch
    {
        InventoryStatus.Depoda => "#22A06B",
        InventoryStatus.Zimmetli => "#0C66E4",
        InventoryStatus.Bakimda => "#E2B203",
        InventoryStatus.Hurda => "#C9372C",
        InventoryStatus.Kayip => "#C9372C",
        _ => "#5E6C84"
    };

    // ----------------- Liste -----------------
    public async Task<IActionResult> Index(string? q, long? categoryId, byte? status)
    {
        if (Guard() is { } g) return g;

        var query = _db.InventoryItems.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(i => i.Name.Contains(term) || (i.Code != null && i.Code.Contains(term)) || (i.SerialNo != null && i.SerialNo.Contains(term)));
        }
        if (categoryId is { } cid) query = query.Where(i => i.CategoryId == cid);
        if (status is { } st) query = query.Where(i => i.Status == (InventoryStatus)st);

        var items = await query.OrderByDescending(i => i.Id).ToListAsync();
        var cats = await _db.InventoryCategories.ToDictionaryAsync(c => c.Id, c => c.Name);
        var users = await UserNamesAsync();

        var vm = new InventoryIndexVM
        {
            Q = q, CategoryId = categoryId, Status = status,
            Categories = await CategoryOptionsAsync(),
            Statuses = StatusOptions(),
            TotalCount = await _db.InventoryItems.CountAsync(),
            AssignedCount = await _db.InventoryItems.CountAsync(i => i.Status == InventoryStatus.Zimmetli),
            AvailableCount = await _db.InventoryItems.CountAsync(i => i.Status == InventoryStatus.Depoda),
            Items = items.Select(i => new InventoryItemListVM
            {
                Id = i.Id, Name = i.Name, Code = i.Code, SerialNo = i.SerialNo,
                CategoryName = i.CategoryId.HasValue && cats.ContainsKey(i.CategoryId.Value) ? cats[i.CategoryId.Value] : null,
                Status = (byte)i.Status, StatusText = StatusText(i.Status), StatusColor = StatusColor(i.Status),
                HolderName = i.CurrentHolderUserId.HasValue && users.ContainsKey(i.CurrentHolderUserId.Value) ? users[i.CurrentHolderUserId.Value] : null
            }).ToList()
        };
        return View(vm);
    }

    // ----------------- Kalem ekle/düzenle -----------------
    [HttpGet]
    public async Task<IActionResult> ItemEdit(long? id)
    {
        if (Guard() is { } g) return g;
        var vm = new InventoryItemEditVM { Categories = await CategoryOptionsAsync(), Statuses = StatusOptions() };
        if (id is { } iid)
        {
            var i = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == iid);
            if (i is null) return NotFound();
            vm.Id = i.Id; vm.Name = i.Name; vm.CategoryId = i.CategoryId; vm.SerialNo = i.SerialNo;
            vm.Code = i.Code; vm.Status = (byte)i.Status; vm.Notes = i.Notes;
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ItemEdit(InventoryItemEditVM model)
    {
        if (Guard() is { } g) return g;
        if (string.IsNullOrWhiteSpace(model.Name)) ModelState.AddModelError(nameof(model.Name), "Ad zorunludur.");
        if (!ModelState.IsValid)
        {
            model.Categories = await CategoryOptionsAsync(); model.Statuses = StatusOptions();
            return View(model);
        }

        InventoryItem item;
        if (model.Id == 0)
        {
            item = new InventoryItem { TenantId = TenantId, CreatedAtUtc = DateTime.UtcNow };
            _db.InventoryItems.Add(item);
        }
        else item = await _db.InventoryItems.FirstAsync(x => x.Id == model.Id);

        item.Name = model.Name.Trim();
        item.CategoryId = model.CategoryId;
        item.SerialNo = model.SerialNo;
        item.Code = model.Code;
        item.Notes = model.Notes;
        // Zimmetli bir kalemin durumu zimmet/iade işlemleriyle yönetilir; elle yalnızca diğer durumlar.
        if (item.Status != InventoryStatus.Zimmetli)
            item.Status = (InventoryStatus)model.Status;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Envanter kalemi kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ItemDelete(long id)
    {
        if (Guard() is { } g) return g;
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id);
        if (item is null) return NotFound();
        if (item.Status == InventoryStatus.Zimmetli)
        {
            TempData["Error"] = "Zimmetli kalem silinemez. Önce iade alın.";
            return RedirectToAction(nameof(Index));
        }
        _db.InventoryItems.Remove(item);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Kalem silindi.";
        return RedirectToAction(nameof(Index));
    }

    // ----------------- Detay + geçmiş -----------------
    public async Task<IActionResult> Details(long id)
    {
        if (Guard() is { } g) return g;
        var i = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id);
        if (i is null) return NotFound();

        var cats = await _db.InventoryCategories.ToDictionaryAsync(c => c.Id, c => c.Name);
        var users = await UserNamesAsync();
        string? UN(long? uid) => uid.HasValue && users.ContainsKey(uid.Value) ? users[uid.Value] : null;

        var history = await _db.InventoryAssignments.AsNoTracking()
            .Where(a => a.InventoryItemId == id)
            .OrderByDescending(a => a.Id)
            .ToListAsync();

        var vm = new InventoryDetailsVM
        {
            Id = i.Id, Name = i.Name, Code = i.Code, SerialNo = i.SerialNo, Notes = i.Notes,
            CategoryName = i.CategoryId.HasValue && cats.ContainsKey(i.CategoryId.Value) ? cats[i.CategoryId.Value] : null,
            StatusText = StatusText(i.Status), StatusColor = StatusColor(i.Status),
            HolderName = UN(i.CurrentHolderUserId),
            IsAssigned = i.Status == InventoryStatus.Zimmetli,
            History = history.Select(a => new InventoryAssignmentRowVM
            {
                AssignedToName = UN(a.AssignedToUserId) ?? $"#{a.AssignedToUserId}",
                AssignedByName = UN(a.AssignedByUserId),
                AssignedAtUtc = a.AssignedAtUtc,
                ReturnedAtUtc = a.ReturnedAtUtc,
                ReturnedByName = UN(a.ReturnedByUserId),
                Notes = a.Notes
            }).ToList()
        };
        return View(vm);
    }

    // ----------------- Kategoriler -----------------
    public async Task<IActionResult> Categories()
    {
        if (Guard() is { } g) return g;
        var cats = await _db.InventoryCategories.OrderBy(c => c.Name).ToListAsync();
        return View(cats);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryCreate(string name)
    {
        if (Guard() is { } g) return g;
        if (!string.IsNullOrWhiteSpace(name))
        {
            _db.InventoryCategories.Add(new InventoryCategory { TenantId = TenantId, Name = name.Trim() });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Kategori eklendi.";
        }
        return RedirectToAction(nameof(Categories));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CategoryDelete(long id)
    {
        if (Guard() is { } g) return g;
        var c = await _db.InventoryCategories.FirstOrDefaultAsync(x => x.Id == id);
        if (c is not null) { _db.InventoryCategories.Remove(c); await _db.SaveChangesAsync(); TempData["Success"] = "Kategori silindi."; }
        return RedirectToAction(nameof(Categories));
    }

    // ----------------- Zimmetle (teslim et) -----------------
    [HttpGet]
    public async Task<IActionResult> Assign(long id)
    {
        if (Guard() is { } g) return g;
        var i = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == id);
        if (i is null) return NotFound();
        if (i.Status == InventoryStatus.Zimmetli)
        {
            TempData["Error"] = "Bu kalem zaten zimmetli. Önce iade alın.";
            return RedirectToAction(nameof(Details), new { id });
        }
        return View(new InventoryAssignVM
        {
            ItemId = i.Id, ItemName = i.Name,
            AssignedByUserId = _currentUser.UserId,
            Users = await UserOptionsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(InventoryAssignVM model)
    {
        if (Guard() is { } g) return g;
        if (model.AssignedToUserId == 0) ModelState.AddModelError(nameof(model.AssignedToUserId), "Teslim edilecek kişi seçilmelidir.");
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == model.ItemId);
        if (item is null) return NotFound();
        if (!ModelState.IsValid)
        {
            model.ItemName = item.Name; model.Users = await UserOptionsAsync();
            return View(model);
        }

        _db.InventoryAssignments.Add(new InventoryAssignment
        {
            TenantId = TenantId,
            InventoryItemId = item.Id,
            AssignedToUserId = model.AssignedToUserId,
            AssignedByUserId = model.AssignedByUserId ?? _currentUser.UserId,
            AssignedAtUtc = DateTime.UtcNow,
            Notes = model.Notes
        });
        item.Status = InventoryStatus.Zimmetli;
        item.CurrentHolderUserId = model.AssignedToUserId;
        await _db.SaveChangesAsync();

        TempData["Success"] = "Zimmet/teslim kaydedildi.";
        return RedirectToAction(nameof(Details), new { id = item.Id });
    }

    // ----------------- İade al (tek kalem) -----------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Return(long itemId)
    {
        if (Guard() is { } g) return g;
        await ReturnItemAsync(itemId);
        await _db.SaveChangesAsync();
        TempData["Success"] = "İade alındı.";
        return RedirectToAction(nameof(Details), new { id = itemId });
    }

    private async Task ReturnItemAsync(long itemId)
    {
        var item = await _db.InventoryItems.FirstOrDefaultAsync(x => x.Id == itemId);
        if (item is null) return;
        var active = await _db.InventoryAssignments
            .Where(a => a.InventoryItemId == itemId && a.ReturnedAtUtc == null)
            .OrderByDescending(a => a.Id).FirstOrDefaultAsync();
        if (active is not null)
        {
            active.ReturnedAtUtc = DateTime.UtcNow;
            active.ReturnedByUserId = _currentUser.UserId;
        }
        item.Status = InventoryStatus.Depoda;
        item.CurrentHolderUserId = null;
    }

    // ----------------- Zimmet listesi (kimde ne var) -----------------
    public async Task<IActionResult> Zimmetler()
    {
        if (Guard() is { } g) return g;
        var vm = new ZimmetlerVM { Rows = await ActiveAssignmentsAsync(null) };
        return View(vm);
    }

    // ----------------- Personel iadesi (işten çıkış) -----------------
    [HttpGet]
    public async Task<IActionResult> PersonelIade(long? userId)
    {
        if (Guard() is { } g) return g;
        var vm = new PersonelIadeVM
        {
            SelectedUserId = userId,
            Users = await UserOptionsAsync(),
            Items = userId is { } uid ? await ActiveAssignmentsAsync(uid) : new()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PersonelIade(long userId, long[]? assignmentIds, bool all = false)
    {
        if (Guard() is { } g) return g;

        var actives = await _db.InventoryAssignments
            .Where(a => a.AssignedToUserId == userId && a.ReturnedAtUtc == null)
            .ToListAsync();
        if (!all && assignmentIds is { Length: > 0 })
            actives = actives.Where(a => assignmentIds.Contains(a.Id)).ToList();

        foreach (var a in actives)
            await ReturnItemAsync(a.InventoryItemId);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"{actives.Count} kalem iade alındı.";
        return RedirectToAction(nameof(PersonelIade), new { userId });
    }

    private async Task<List<ActiveAssignmentRowVM>> ActiveAssignmentsAsync(long? userId)
    {
        var q = _db.InventoryAssignments.AsNoTracking().Where(a => a.ReturnedAtUtc == null);
        if (userId is { } uid) q = q.Where(a => a.AssignedToUserId == uid);
        var actives = await q.OrderBy(a => a.AssignedToUserId).ThenByDescending(a => a.Id).ToListAsync();

        var itemIds = actives.Select(a => a.InventoryItemId).Distinct().ToList();
        var items = await _db.InventoryItems.AsNoTracking().Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.Code, i.CategoryId }).ToListAsync();
        var imap = items.ToDictionary(x => x.Id);
        var cats = await _db.InventoryCategories.ToDictionaryAsync(c => c.Id, c => c.Name);
        var users = await UserNamesAsync();

        return actives.Select(a => new ActiveAssignmentRowVM
        {
            AssignmentId = a.Id,
            ItemId = a.InventoryItemId,
            ItemName = imap.TryGetValue(a.InventoryItemId, out var it) ? it.Name : $"#{a.InventoryItemId}",
            Code = imap.TryGetValue(a.InventoryItemId, out var it2) ? it2.Code : null,
            CategoryName = imap.TryGetValue(a.InventoryItemId, out var it3) && it3.CategoryId.HasValue && cats.ContainsKey(it3.CategoryId.Value) ? cats[it3.CategoryId.Value] : null,
            UserId = a.AssignedToUserId,
            UserName = users.TryGetValue(a.AssignedToUserId, out var un) ? un : $"#{a.AssignedToUserId}",
            AssignedAtUtc = a.AssignedAtUtc
        }).ToList();
    }

    // ----------------- Sayım -----------------
    public async Task<IActionResult> Sayim()
    {
        if (Guard() is { } g) return g;
        var counts = await _db.InventoryCounts.AsNoTracking().OrderByDescending(c => c.Id).ToListAsync();
        var lineStats = await _db.InventoryCountLines.AsNoTracking()
            .GroupBy(l => l.InventoryCountId)
            .Select(grp => new { CountId = grp.Key, Total = grp.Count(), Found = grp.Count(x => x.IsFound) })
            .ToListAsync();
        var smap = lineStats.ToDictionary(x => x.CountId);

        var vm = new SayimListVM
        {
            Counts = counts.Select(c => new SayimRowVM
            {
                Id = c.Id, Name = c.Name, CreatedAtUtc = c.CreatedAtUtc, CompletedAtUtc = c.CompletedAtUtc,
                Total = smap.TryGetValue(c.Id, out var s) ? s.Total : 0,
                Found = smap.TryGetValue(c.Id, out var s2) ? s2.Found : 0
            }).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SayimCreate(string name)
    {
        if (Guard() is { } g) return g;
        var count = new InventoryCount
        {
            TenantId = TenantId,
            Name = string.IsNullOrWhiteSpace(name) ? $"Sayım {DateTime.Today:dd.MM.yyyy}" : name.Trim(),
            CountedByUserId = _currentUser.UserId,
            CreatedAtUtc = DateTime.UtcNow
        };
        _db.InventoryCounts.Add(count);
        await _db.SaveChangesAsync();

        var itemIds = await _db.InventoryItems.Select(i => i.Id).ToListAsync();
        foreach (var iid in itemIds)
            _db.InventoryCountLines.Add(new InventoryCountLine { InventoryCountId = count.Id, InventoryItemId = iid, IsFound = false });
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(SayimDetay), new { id = count.Id });
    }

    [HttpGet]
    public async Task<IActionResult> SayimDetay(long id)
    {
        if (Guard() is { } g) return g;
        var count = await _db.InventoryCounts.FirstOrDefaultAsync(c => c.Id == id);
        if (count is null) return NotFound();

        var lines = await _db.InventoryCountLines.AsNoTracking().Where(l => l.InventoryCountId == id).ToListAsync();
        var itemIds = lines.Select(l => l.InventoryItemId).ToList();
        var items = await _db.InventoryItems.AsNoTracking().Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.Code, i.CurrentHolderUserId }).ToListAsync();
        var imap = items.ToDictionary(x => x.Id);
        var users = await UserNamesAsync();

        var vm = new SayimDetayVM
        {
            Id = count.Id, Name = count.Name, Completed = count.CompletedAtUtc != null,
            Lines = lines.Select(l => new SayimLineVM
            {
                LineId = l.Id,
                ItemName = imap.TryGetValue(l.InventoryItemId, out var it) ? it.Name : $"#{l.InventoryItemId}",
                Code = imap.TryGetValue(l.InventoryItemId, out var it2) ? it2.Code : null,
                HolderName = imap.TryGetValue(l.InventoryItemId, out var it3) && it3.CurrentHolderUserId.HasValue && users.ContainsKey(it3.CurrentHolderUserId.Value) ? users[it3.CurrentHolderUserId.Value] : null,
                IsFound = l.IsFound
            }).OrderBy(x => x.ItemName).ToList()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SayimSave(long id, long[]? foundLineIds)
    {
        if (Guard() is { } g) return g;
        var found = (foundLineIds ?? Array.Empty<long>()).ToHashSet();
        var lines = await _db.InventoryCountLines.Where(l => l.InventoryCountId == id).ToListAsync();
        foreach (var l in lines) l.IsFound = found.Contains(l.Id);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Sayım kaydedildi.";
        return RedirectToAction(nameof(SayimDetay), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SayimComplete(long id)
    {
        if (Guard() is { } g) return g;
        var count = await _db.InventoryCounts.FirstOrDefaultAsync(c => c.Id == id);
        if (count is not null) { count.CompletedAtUtc = DateTime.UtcNow; await _db.SaveChangesAsync(); TempData["Success"] = "Sayım tamamlandı."; }
        return RedirectToAction(nameof(SayimDetay), new { id });
    }
}
