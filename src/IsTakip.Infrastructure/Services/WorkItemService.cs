using IsTakip.Application.Common;
using IsTakip.Application.WorkItems;
using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Infrastructure.Services;

/// <summary>WorkItem (görev/talep) dikey diliminin iş mantığı. Controller'lar ince kalsın diye buraya toplanır.</summary>
public class WorkItemService : IWorkItemService
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public WorkItemService(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // "Yönetici" yetkisi: silme ve kapanış/tamamlama işlemleri buna bağlıdır.
    // Silme izni (WorkItem.Delete) bu projede yönetici göstergesi olarak kullanılır.
    private const string ManagerPermission = "WorkItem.Delete";
    private bool IsManager => _currentUser.HasPermission(ManagerPermission);

    public async Task<PagedResult<WorkItemListItemDto>> GetListAsync(WorkItemFilter filter, CancellationToken ct = default)
    {
        var query = _db.WorkItems.AsNoTracking();

        if (filter.WorkItemTypeId is { } typeId) query = query.Where(w => w.WorkItemTypeId == typeId);
        if (filter.StateId is { } stateId) query = query.Where(w => w.CurrentStateId == stateId);
        if (filter.AssigneeUserId is { } assignee) query = query.Where(w => w.AssigneeUserId == assignee);
        if (filter.DepartmentId is { } dept) query = query.Where(w => w.DepartmentId == dept);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(w => w.Title.Contains(term) || w.Key.Contains(term));
        }

        var total = await query.CountAsync(ct);

        var page = filter.Page < 1 ? 1 : filter.Page;
        var size = filter.PageSize is < 1 or > 200 ? 25 : filter.PageSize;

        var items = await query
            .OrderByDescending(w => w.CreatedAtUtc)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(ToListItem)
            .ToListAsync(ct);

        return new PagedResult<WorkItemListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = size
        };
    }

    public async Task<WorkItemDetailDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        return await _db.WorkItems.AsNoTracking()
            .Where(w => w.Id == id)
            .Select(w => new WorkItemDetailDto(
                w.Id,
                w.Key,
                w.Title,
                w.Description,
                w.Type.Name,
                w.CurrentState.Name,
                w.CurrentStateId,
                w.Priority != null ? w.Priority.Name : null,
                w.Assignee != null ? w.Assignee.FirstName + " " + w.Assignee.LastName : null,
                w.Reporter != null ? w.Reporter.FirstName + " " + w.Reporter.LastName : null,
                w.StartDate,
                w.DueDate,
                w.CreatedAtUtc))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Result<long>> CreateAsync(CreateWorkItemRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<long>.Fail("Başlık zorunludur.");

        var tenantId = _currentUser.TenantId
            ?? throw new InvalidOperationException("Aktif kiracı bulunamadı.");

        var type = await _db.WorkItemTypes
            .FirstOrDefaultAsync(t => t.Id == request.WorkItemTypeId, ct);
        if (type is null) return Result<long>.Fail("Geçersiz görev türü.");

        var workflowId = type.DefaultWorkflowId
            ?? (await _db.Workflows.Where(w => w.IsActive).Select(w => (long?)w.Id).FirstOrDefaultAsync(ct));
        if (workflowId is null) return Result<long>.Fail("Türe bağlı bir iş akışı tanımlı değil.");

        var initialState = await _db.WorkflowStates
            .Where(s => s.WorkflowId == workflowId && s.IsInitial)
            .OrderBy(s => s.SortOrder)
            .FirstOrDefaultAsync(ct);
        if (initialState is null) return Result<long>.Fail("İş akışında başlangıç durumu tanımlı değil.");

        var key = await GenerateKeyAsync(type, ct);

        var workItem = new WorkItem
        {
            TenantId = tenantId,
            Key = key,
            WorkItemTypeId = type.Id,
            Title = request.Title.Trim(),
            Description = request.Description,
            PriorityId = request.PriorityId,
            AssigneeUserId = request.AssigneeUserId,
            ReporterUserId = _currentUser.UserId,
            DepartmentId = request.DepartmentId,
            ProjectId = request.ProjectId,
            DueDate = request.DueDate,
            WorkflowId = workflowId.Value,
            CurrentStateId = initialState.Id
        };

        _db.WorkItems.Add(workItem);
        await _db.SaveChangesAsync(ct);

        _db.WorkItemStateHistory.Add(new WorkItemStateHistory
        {
            WorkItemId = workItem.Id,
            FromStateId = null,
            ToStateId = initialState.Id,
            ChangedByUserId = _currentUser.UserId,
            ChangedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);

        return Result<long>.Success(workItem.Id);
    }

    public async Task<Result> ChangeStateAsync(long workItemId, long toStateId, CancellationToken ct = default)
    {
        var workItem = await _db.WorkItems.FirstOrDefaultAsync(w => w.Id == workItemId, ct);
        if (workItem is null) return Result.Fail("Kayıt bulunamadı.");

        var targetState = await _db.WorkflowStates
            .FirstOrDefaultAsync(s => s.Id == toStateId && s.WorkflowId == workItem.WorkflowId, ct);
        if (targetState is null) return Result.Fail("Hedef durum bu iş akışına ait değil.");

        // Kapanış/tamamlanma kategorisindeki durumlara (Tamamlandı, İptal, Reddedildi)
        // yalnızca yöneticiler alabilir. Diğer tüm durumları herkes değiştirebilir.
        if (targetState.Category == StateCategory.Tamamlandi && !IsManager)
            return Result.Fail("Bu duruma yalnızca yöneticiler alabilir (tamamlama/kapatma yetkisi gerekir).");

        var allowed = await _db.WorkflowTransitions.AnyAsync(
            t => t.WorkflowId == workItem.WorkflowId &&
                 t.FromStateId == workItem.CurrentStateId &&
                 t.ToStateId == toStateId, ct);

        // Geçiş tanımı yoksa serbest bırak (panodan hızlı taşıma); tanım varsa kurala uy.
        var hasAnyTransition = await _db.WorkflowTransitions
            .AnyAsync(t => t.WorkflowId == workItem.WorkflowId && t.FromStateId == workItem.CurrentStateId, ct);
        if (hasAnyTransition && !allowed)
            return Result.Fail("Bu durum geçişine izin verilmiyor.");

        var fromStateId = workItem.CurrentStateId;
        workItem.CurrentStateId = toStateId;
        workItem.CompletedAtUtc = targetState.Category == StateCategory.Tamamlandi ? DateTime.UtcNow : null;

        _db.WorkItemStateHistory.Add(new WorkItemStateHistory
        {
            WorkItemId = workItem.Id,
            FromStateId = fromStateId,
            ToStateId = toStateId,
            ChangedByUserId = _currentUser.UserId,
            ChangedAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<IReadOnlyList<BoardColumnDto>> GetBoardAsync(long? workItemTypeId, CancellationToken ct = default)
    {
        long? workflowId = null;
        if (workItemTypeId is { } typeId)
            workflowId = await _db.WorkItemTypes.Where(t => t.Id == typeId)
                .Select(t => t.DefaultWorkflowId).FirstOrDefaultAsync(ct);

        workflowId ??= await _db.Workflows.Where(w => w.IsActive)
            .OrderBy(w => w.Id).Select(w => (long?)w.Id).FirstOrDefaultAsync(ct);

        if (workflowId is null) return Array.Empty<BoardColumnDto>();

        var states = await _db.WorkflowStates.AsNoTracking()
            .Where(s => s.WorkflowId == workflowId)
            .OrderBy(s => s.SortOrder)
            .ToListAsync(ct);

        var itemsQuery = _db.WorkItems.AsNoTracking().Where(w => w.WorkflowId == workflowId);
        if (workItemTypeId is { } t2) itemsQuery = itemsQuery.Where(w => w.WorkItemTypeId == t2);

        var items = await itemsQuery.Select(ToListItem).ToListAsync(ct);

        return states.Select(s => new BoardColumnDto(
            s.Id,
            s.Name,
            s.ColorHex,
            items.Where(i => i.CurrentStateId == s.Id).ToList())).ToList();
    }

    public async Task<IReadOnlyList<WorkItemStateOptionDto>> GetStatesAsync(long workItemId, CancellationToken ct = default)
    {
        var workflowId = await _db.WorkItems.Where(w => w.Id == workItemId)
            .Select(w => (long?)w.WorkflowId).FirstOrDefaultAsync(ct);
        if (workflowId is null) return Array.Empty<WorkItemStateOptionDto>();

        return await _db.WorkflowStates.AsNoTracking()
            .Where(s => s.WorkflowId == workflowId)
            .OrderBy(s => s.SortOrder)
            .Select(s => new WorkItemStateOptionDto(s.Id, s.Name, s.Category, s.ColorHex))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CommentDto>> GetCommentsAsync(long workItemId, CancellationToken ct = default)
    {
        return await (
            from c in _db.Comments.AsNoTracking()
            join u in _db.Users on c.AuthorUserId equals u.Id
            where c.WorkItemId == workItemId
            orderby c.CreatedAtUtc
            select new CommentDto(c.Id, u.FirstName + " " + u.LastName, c.Body, c.CreatedAtUtc))
            .ToListAsync(ct);
    }

    public async Task<Result> AddCommentAsync(long workItemId, string body, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(body))
            return Result.Fail("Yorum boş olamaz.");

        var tenantId = _currentUser.TenantId ?? throw new InvalidOperationException("Aktif kiracı bulunamadı.");
        var userId = _currentUser.UserId ?? throw new InvalidOperationException("Aktif kullanıcı bulunamadı.");

        var exists = await _db.WorkItems.AnyAsync(w => w.Id == workItemId, ct);
        if (!exists) return Result.Fail("Kayıt bulunamadı.");

        _db.Comments.Add(new Comment
        {
            TenantId = tenantId,
            WorkItemId = workItemId,
            AuthorUserId = userId,
            Body = body.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(long workItemId, CancellationToken ct = default)
    {
        if (!IsManager)
            return Result.Fail("Kaydı silme yetkiniz yok (yalnızca yöneticiler silebilir).");

        var workItem = await _db.WorkItems.FirstOrDefaultAsync(w => w.Id == workItemId, ct);
        if (workItem is null) return Result.Fail("Kayıt bulunamadı.");

        // Interceptor bu Remove'u soft-delete'e (IsDeleted = true) çevirir ve denetim logu yazar.
        _db.WorkItems.Remove(workItem);
        await _db.SaveChangesAsync(ct);
        return Result.Success();
    }

    private async Task<string> GenerateKeyAsync(WorkItemType type, CancellationToken ct)
    {
        var prefix = string.IsNullOrWhiteSpace(type.KeyPrefix) ? "GRV" : type.KeyPrefix!.Trim();
        var count = await _db.WorkItems.IgnoreQueryFilters()
            .CountAsync(w => w.TenantId == type.TenantId && w.WorkItemTypeId == type.Id, ct);
        return $"{prefix}-{count + 1}";
    }

    // Liste/pano kartı projeksiyonu (tek yerde tanımlı, tekrar yok).
    private static readonly System.Linq.Expressions.Expression<Func<WorkItem, WorkItemListItemDto>> ToListItem =
        w => new WorkItemListItemDto(
            w.Id,
            w.Key,
            w.Title,
            w.Type.Name,
            w.Type.ColorHex,
            w.CurrentState.Name,
            w.CurrentState.ColorHex,
            w.CurrentStateId,
            w.Priority != null ? w.Priority.Name : null,
            w.Priority != null ? w.Priority.ColorHex : null,
            w.Assignee != null ? w.Assignee.FirstName + " " + w.Assignee.LastName : null,
            w.DueDate);
}
