namespace IsTakip.Application.Common;

/// <summary>Aktif isteğin kullanıcı/kiracı bağlamı. Web katmanında HttpContext'ten beslenir.</summary>
public interface ICurrentUserService
{
    long? UserId { get; }
    long? TenantId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permissionKey);
}
