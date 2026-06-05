using Microsoft.AspNetCore.Mvc.Rendering;

namespace IsTakip.Web.Models;

public class ApprovalStartVM
{
    public long WorkItemId { get; set; }
    public string WorkItemKey { get; set; } = default!;
    public string Title { get; set; } = default!;
    public List<long?> ApproverUserIds { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}

public class MyApprovalRowVM
{
    public long StepId { get; set; }
    public long WorkItemId { get; set; }
    public string WorkItemKey { get; set; } = default!;
    public string Title { get; set; } = default!;
    public int StepOrder { get; set; }
    public DateTime RequestedAtUtc { get; set; }
}

public class ApprovalInfoVM
{
    public long RequestId { get; set; }
    public string StatusText { get; set; } = default!;
    public string StatusColor { get; set; } = "#5E6C84";
    public List<ApprovalStepVM> Steps { get; set; } = new();
}

public class ApprovalStepVM
{
    public int Order { get; set; }
    public string ApproverName { get; set; } = default!;
    public string StatusText { get; set; } = default!;
    public string StatusColor { get; set; } = "#5E6C84";
    public string? Comment { get; set; }
    public DateTime? DecidedAtUtc { get; set; }
    public bool IsMyActiveStep { get; set; }
    public long StepId { get; set; }
}
