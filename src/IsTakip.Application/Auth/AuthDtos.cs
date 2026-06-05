namespace IsTakip.Application.Auth;

public record LoggedInUserDto(long UserId, string UserName, string FullName, IReadOnlyList<string> Permissions);
