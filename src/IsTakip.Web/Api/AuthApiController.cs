using System.Security.Claims;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Web.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthApiController(UserManager<AppUser> userManager, AppDbContext db, JwtTokenService jwt)
    {
        _userManager = userManager;
        _db = db;
        _jwt = jwt;
    }

    public record LoginRequest(string UserName, string Password);

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user is null || user.IsDeleted
            || !await _userManager.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { error = "Kullanıcı adı veya parola hatalı." });

        var permissionKeys = await (
            from ur in _db.UserRoles
            join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in _db.Permissions on rp.PermissionId equals p.Id
            where ur.UserId == user.Id
            select p.Key).Distinct().ToListAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName!),
            new(CurrentUserService.TenantClaim, user.TenantId.ToString())
        };
        claims.AddRange(permissionKeys.Select(k => new Claim(CurrentUserService.PermissionClaim, k)));

        return Ok(new
        {
            token = _jwt.CreateToken(claims),
            user = new { user.Id, user.UserName, fullName = user.FullName }
        });
    }
}
