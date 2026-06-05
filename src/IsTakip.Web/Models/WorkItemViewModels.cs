using System.ComponentModel.DataAnnotations;
using IsTakip.Application.WorkItems;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace IsTakip.Web.Models;

public class WorkItemListViewModel
{
    public PagedResultView Result { get; set; } = new();
    public WorkItemFilter Filter { get; set; } = new();
    public IReadOnlyList<WorkItemListItemDto> Items { get; set; } = Array.Empty<WorkItemListItemDto>();
    public int TotalCount { get; set; }
}

public class PagedResultView
{
    public int TotalCount { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class CreateWorkItemViewModel
{
    [Required(ErrorMessage = "Tür seçilmelidir.")]
    [Display(Name = "Tür")]
    public long WorkItemTypeId { get; set; }

    // Alt görev olarak oluşturuluyorsa üst görevin Id'si.
    public long? ParentWorkItemId { get; set; }
    public string? ParentKey { get; set; }

    // Form Tasarımcısı ile tanımlanan dinamik alanlar.
    public List<CustomFieldInputVM> CustomFieldInputs { get; set; } = new();
    public Dictionary<long, string?> CustomFields { get; set; } = new();

    [Required(ErrorMessage = "Başlık zorunludur.")]
    [StringLength(300)]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = default!;

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Öncelik")]
    public long? PriorityId { get; set; }

    [Display(Name = "Sorumlu")]
    public long? AssigneeUserId { get; set; }

    [Display(Name = "Departman")]
    public long? DepartmentId { get; set; }

    [Display(Name = "Son Tarih")]
    [DataType(DataType.Date)]
    public DateOnly? DueDate { get; set; }

    public List<SelectListItem> Types { get; set; } = new();
    public List<SelectListItem> Priorities { get; set; } = new();
    public List<SelectListItem> Departments { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}

public class BoardViewModel
{
    public long? WorkItemTypeId { get; set; }
    public IReadOnlyList<BoardColumnDto> Columns { get; set; } = Array.Empty<BoardColumnDto>();
    public List<SelectListItem> Types { get; set; } = new();
}

public class WorkItemDetailsPageVM
{
    public WorkItemDetailDto Item { get; set; } = default!;
    public IReadOnlyList<WorkItemStateOptionDto> States { get; set; } = Array.Empty<WorkItemStateOptionDto>();
    public IReadOnlyList<CommentDto> Comments { get; set; } = Array.Empty<CommentDto>();
    public bool IsManager { get; set; }
    public IReadOnlyList<SubtaskDto> Subtasks { get; set; } = Array.Empty<SubtaskDto>();
    public List<CustomFieldValueVM> CustomFields { get; set; } = new();
    public ApprovalInfoVM? Approval { get; set; }
    public List<AttachmentVM> Attachments { get; set; } = new();
    public string? SlaText { get; set; }
    public string SlaColor { get; set; } = "#5E6C84";
}

public class AttachmentVM
{
    public long Id { get; set; }
    public string FileName { get; set; } = default!;
    public string SizeText { get; set; } = default!;
    public string? UploaderName { get; set; }
    public DateTime UploadedAtUtc { get; set; }
}
