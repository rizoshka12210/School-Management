using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SchoolManagementSystem.Web.Data;
using SchoolManagementSystem.Web.Models.Identity;

namespace SchoolManagementSystem.Web.Tests;

/// <summary>
/// Builds a real Identity + EF Core stack backed by a fresh, uniquely
/// named InMemory database, so tests exercise the same UserManager and
/// AppDbContext wiring the app uses at runtime instead of hand-rolled
/// fakes. Each call gets its own isolated database.
/// </summary>
public static class TestServiceProviderFactory
{
    public static ServiceProvider Create()
    {
        var services = new ServiceCollection();

        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services
            .AddIdentity<ApplicationUser, IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddLogging();

        return services.BuildServiceProvider();
    }
}
