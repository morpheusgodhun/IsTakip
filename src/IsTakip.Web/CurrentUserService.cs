using System.Security.Claims;
using IsTakip.Application.Common;
using Microsoft.AspNetCore.Http;

namespace IsTakip.Web;

/// <summary>HttpContext üzerindeki claim'lerden aktif kullanıcı/kiracı bağlamını okur.</summary>
public class CurrentUserService : ICurrentUserService
{
    public const string TenantClaim = "tenant_id";
    public const string PermissionClaim = "permission";

    private readonly IHttpContextAccessor _accessor;

    public CurrentUserService(IHttpContextAccessor accessor) => _accessor = accessor;

    private ClaimsPrincipal? Principal => _accessor.HttpContext?.User;

    public long? UserId =>
        long.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public long? TenantId =>
        long.TryParse(Principal?.FindFirstValue(TenantClaim), out var id) ? id : null;

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool HasPermission(string permissionKey) =>
        Principal?.HasClaim(PermissionClaim, permissionKey) ?? false;
}
