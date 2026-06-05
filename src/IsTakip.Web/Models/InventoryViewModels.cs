using Microsoft.AspNetCore.Mvc.Rendering;

namespace IsTakip.Web.Models;

public class InventoryItemListVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public string? SerialNo { get; set; }
    public string? CategoryName { get; set; }
    public string StatusText { get; set; } = default!;
    public string StatusColor { get; set; } = "#5E6C84";
    public byte Status { get; set; }
    public string? HolderName { get; set; }
}

public class InventoryIndexVM
{
    public List<InventoryItemListVM> Items { get; set; } = new();
    public string? Q { get; set; }
    public long? CategoryId { get; set; }
    public byte? Status { get; set; }
    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
    public int TotalCount { get; set; }
    public int AssignedCount { get; set; }
    public int AvailableCount { get; set; }
}

public class InventoryItemEditVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public long? CategoryId { get; set; }
    public string? SerialNo { get; set; }
    public string? Code { get; set; }
    public byte Status { get; set; } = 1;
    public string? Notes { get; set; }
    public List<SelectListItem> Categories { get; set; } = new();
    public List<SelectListItem> Statuses { get; set; } = new();
}

public class InventoryAssignmentRowVM
{
    public string AssignedToName { get; set; } = default!;
    public string? AssignedByName { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? ReturnedAtUtc { get; set; }
    public string? ReturnedByName { get; set; }
    public string? Notes { get; set; }
    public bool IsActive => ReturnedAtUtc == null;
}

public class InventoryDetailsVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Code { get; set; }
    public string? SerialNo { get; set; }
    public string? CategoryName { get; set; }
    public string StatusText { get; set; } = default!;
    public string StatusColor { get; set; } = "#5E6C84";
    public string? HolderName { get; set; }
    public string? Notes { get; set; }
    public bool IsAssigned { get; set; }
    public List<InventoryAssignmentRowVM> History { get; set; } = new();
}

public class InventoryAssignVM
{
    public long ItemId { get; set; }
    public string ItemName { get; set; } = default!;
    public long AssignedToUserId { get; set; }
    public long? AssignedByUserId { get; set; }
    public string? Notes { get; set; }
    public List<SelectListItem> Users { get; set; } = new();
}

public class ActiveAssignmentRowVM
{
    public long AssignmentId { get; set; }
    public long ItemId { get; set; }
    public string ItemName { get; set; } = default!;
    public string? Code { get; set; }
    public string? CategoryName { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = default!;
    public DateTime AssignedAtUtc { get; set; }
}

public class ZimmetlerVM
{
    public List<ActiveAssignmentRowVM> Rows { get; set; } = new();
}

public class PersonelIadeVM
{
    public long? SelectedUserId { get; set; }
    public List<SelectListItem> Users { get; set; } = new();
    public List<ActiveAssignmentRowVM> Items { get; set; } = new();
}

public class SayimListVM
{
    public List<SayimRowVM> Counts { get; set; } = new();
}

public class SayimRowVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int Total { get; set; }
    public int Found { get; set; }
}

public class SayimDetayVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public bool Completed { get; set; }
    public List<SayimLineVM> Lines { get; set; } = new();
}

public class SayimLineVM
{
    public long LineId { get; set; }
    public string ItemName { get; set; } = default!;
    public string? Code { get; set; }
    public string? HolderName { get; set; }
    public bool IsFound { get; set; }
}
