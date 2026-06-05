using IsTakip.Application.WorkItems;
using IsTakip.Domain.Entities;
using IsTakip.Infrastructure.Persistence;
using IsTakip.Infrastructure.Persistence.Interceptors;
using IsTakip.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IsTakip.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("Default"),
                sql => sql.EnableRetryOnFailure());
            options.AddInterceptors(sp.GetRequiredService<AuditableEntityInterceptor>());
        });
        services
    .AddIdentity<AppUser, AppRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();


        services.AddScoped<IWorkItemService, WorkItemService>();
        services.AddScoped<ApplicationDbSeeder>();

        return services;
    }
}
