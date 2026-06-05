using IsTakip.Domain.Common;

namespace IsTakip.Domain.Entities;

public class Company : AuditableTenantEntity
{
    public string Name { get; set; } = default!;
    public string? TaxNumber { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Location : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public string? City { get; set; }
    public string? Address { get; set; }
    public bool IsDeleted { get; set; }
}

public class Branch : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public long CompanyId { get; set; }
    public long? LocationId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public Company Company { get; set; } = default!;
    public Location? Location { get; set; }
}

public class Department : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public long? BranchId { get; set; }
    public string Name { get; set; } = default!;
    public long? ManagerUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; }
    public Branch? Branch { get; set; }
    public AppUser? Manager { get; set; }
}

public class Unit : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public long DepartmentId { get; set; }
    public string Name { get; set; } = default!;
    public bool IsDeleted { get; set; }
    public Department Department { get; set; } = default!;
}

public class Team : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public long? DepartmentId { get; set; }
    public string Name { get; set; } = default!;
    public long? LeadUserId { get; set; }
    public bool IsDeleted { get; set; }
    public Department? Department { get; set; }
    public ICollection<TeamMember> Members { get; set; } = new List<TeamMember>();
}

public class TeamMember
{
    public long TeamId { get; set; }
    public long UserId { get; set; }
    public Team Team { get; set; } = default!;
    public AppUser User { get; set; } = default!;
}
