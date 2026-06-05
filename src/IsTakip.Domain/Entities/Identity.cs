using IsTakip.Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace IsTakip.Domain.Entities;

/// <summary>Uygulama kullanıcısı. ASP.NET Core Identity (long anahtar) ile dbo.Users tablosuna map edilir.</summary>
public class AppUser : IdentityUser<long>, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public string? Title { get; set; }
    public long? DepartmentId { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Aktif;

    public DateTime CreatedAtUtc { get; set; }
    public long? CreatedByUserId { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public long? UpdatedByUserId { get; set; }
    public bool IsDeleted { get; set; }

    public Department? Department { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}

/// <summary>Dinamik rol. dbo.Roles tablosuna map edilir. IsSystem = silinemez.</summary>
public class AppRole : IdentityRole<long>, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>İzin kataloğu. Sistem geneli (tenant bağımsız). Anahtar koddaki sabitlerle eşleşir.</summary>
public class Permission : BaseEntity
{
    public string Key { get; set; } = default!;
    public string Module { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public int SortOrder { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

public class RolePermission
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public AppRole Role { get; set; } = default!;
    public Permission Permission { get; set; } = default!;
}

public class RefreshToken : BaseEntity
{
    public long UserId { get; set; }
    public string Token { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedByIp { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByToken { get; set; }
    public bool IsActive => RevokedAtUtc is null && DateTime.UtcNow < ExpiresAtUtc;
}

public class UserSession : BaseEntity
{
    public long UserId { get; set; }
    public DateTime LoginAtUtc { get; set; }
    public DateTime? LogoutAtUtc { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsActive { get; set; } = true;
}
