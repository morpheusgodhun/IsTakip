using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IsTakip.Web.Hubs;

/// <summary>
/// Anlık bildirim kanalı. Varsayılan IUserIdProvider, NameIdentifier claim'ini kullanır;
/// bu projede giriş sırasında bu claim kullanıcının Id'si olarak set edilir, dolayısıyla
/// Clients.User(userId) doğru kullanıcıya ulaşır.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
}
