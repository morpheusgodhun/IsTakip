namespace IsTakip.Application.WorkItems;

public record WorkItemListItemDto(
    long Id,
    string Key,
    string Title,
    string TypeName,
    string? TypeColor,
    string StateName,
    string? StateColor,
    long CurrentStateId,
    string? PriorityName,
    string? PriorityColor,
    string? AssigneeName,
    DateOnly? DueDate);

public record WorkItemDetailDto(
    long Id,
    string Key,
    string Title,
    string? Description,
    string TypeName,
    string StateName,
    long CurrentStateId,
    string? PriorityName,
    string? AssigneeName,
    string? ReporterName,
    DateOnly? StartDate,
    DateOnly? DueDate,
    DateTime CreatedAtUtc);

/// <summary>Yeni iş kaydı oluşturma girdisi.</summary>
public class CreateWorkItemRequest
{
    public long WorkItemTypeId { get; set; }
    public long? ParentWorkItemId { get; set; }
    public Dictionary<long, string?> CustomFields { get; set; } = new();
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public long? PriorityId { get; set; }
    public long? AssigneeUserId { get; set; }
    public long? DepartmentId { get; set; }
    public long? ProjectId { get; set; }
    public DateOnly? DueDate { get; set; }
}

public class WorkItemFilter
{
    public long? WorkItemTypeId { get; set; }
    public long? StateId { get; set; }
    public long? AssigneeUserId { get; set; }
    public long? DepartmentId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

/// <summary>Kanban panosu sütunu (durum) ve içindeki kartlar.</summary>
public record BoardColumnDto(long StateId, string StateName, string? ColorHex, IReadOnlyList<WorkItemListItemDto> Items);

/// <summary>Durum değiştirme seçeneği (detay sayfasındaki durum menüsü için).</summary>
public record WorkItemStateOptionDto(long Id, string Name, IsTakip.Domain.Common.StateCategory Category, string? ColorHex);

/// <summary>Bir iş kaydındaki yorum.</summary>
public record CommentDto(long Id, string AuthorName, string Body, DateTime CreatedAtUtc);

/// <summary>Alt görev özeti (detay sayfasında listelenir).</summary>
public record SubtaskDto(long Id, string Key, string Title, string StateName, string? StateColor, bool IsDone);
