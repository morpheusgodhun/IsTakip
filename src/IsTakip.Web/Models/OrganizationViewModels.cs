using Microsoft.AspNetCore.Mvc.Rendering;

namespace IsTakip.Web.Models;

public class OrgTreeVM
{
    public List<OrgCompany> Companies { get; set; } = new();
}

public class OrgCompany
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; }
    public List<OrgBranch> Branches { get; set; } = new();
}

public class OrgBranch
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public List<OrgDepartment> Departments { get; set; } = new();
}

public class OrgDepartment
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? ManagerName { get; set; }
    public List<OrgTeam> Teams { get; set; } = new();
}

public class OrgTeam
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? LeadName { get; set; }
    public int MemberCount { get; set; }
}

public class CompanyFormVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public class BranchFormVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public long CompanyId { get; set; }
    public long? LocationId { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SelectListItem> Companies { get; set; } = new();
    public List<SelectListItem> Locations { get; set; } = new();
}

public class DepartmentFormVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public long? BranchId { get; set; }
    public long? ManagerUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public List<SelectListItem> Branches { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}

public class TeamFormVM
{
    public long Id { get; set; }
    public string Name { get; set; } = default!;
    public long? DepartmentId { get; set; }
    public long? LeadUserId { get; set; }
    public List<SelectListItem> Departments { get; set; } = new();
    public List<SelectListItem> Users { get; set; } = new();
}
