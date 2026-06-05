namespace IsTakip.Web.Models;

public class RoleListItemVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public int PermissionCount { get; set; }
    public int MemberCount { get; set; }
}

public class PermissionOptionVM
{
    public long Id { get; set; }
    public string Key { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
}

public class PermissionGroupVM
{
    public string Module { get; set; } = default!;
    public List<PermissionOptionVM> Permissions { get; set; } = new();
}

public class RoleEditVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public List<PermissionGroupVM> Groups { get; set; } = new();
    public List<long> SelectedPermissionIds { get; set; } = new();
}
