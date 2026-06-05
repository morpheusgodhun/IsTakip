using IsTakip.Application.Common;

namespace IsTakip.Application.WorkItems;

public interface IWorkItemService
{
    Task<PagedResult<WorkItemListItemDto>> GetListAsync(WorkItemFilter filter, CancellationToken ct = default);
    Task<WorkItemDetailDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Result<long>> CreateAsync(CreateWorkItemRequest request, CancellationToken ct = default);
    Task<Result> ChangeStateAsync(long workItemId, long toStateId, CancellationToken ct = default);
    Task<IReadOnlyList<BoardColumnDto>> GetBoardAsync(long? workItemTypeId, CancellationToken ct = default);

    // Detay sayfası: durum seçenekleri, yorumlar, yorum ekleme ve silme.
    Task<IReadOnlyList<WorkItemStateOptionDto>> GetStatesAsync(long workItemId, CancellationToken ct = default);
    Task<IReadOnlyList<CommentDto>> GetCommentsAsync(long workItemId, CancellationToken ct = default);
    Task<Result> AddCommentAsync(long workItemId, string body, CancellationToken ct = default);
    Task<Result> DeleteAsync(long workItemId, CancellationToken ct = default);
    Task<IReadOnlyList<SubtaskDto>> GetSubtasksAsync(long parentId, CancellationToken ct = default);
}
