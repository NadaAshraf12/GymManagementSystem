using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Linq;

namespace GymManagementSystem.WebUI.Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            var providerDescriptors = services
                .Where(d => d.ServiceType.FullName == "Microsoft.EntityFrameworkCore.Infrastructure.IDatabaseProvider")
                .ToList();
            foreach (var descriptor in providerDescriptors)
            {
                services.Remove(descriptor);
            }

            var sqlServerDescriptors = services
                .Where(d =>
                    (d.ServiceType.FullName?.Contains("SqlServer") ?? false) ||
                    (d.ImplementationType?.FullName?.Contains("SqlServer") ?? false) ||
                    (d.ImplementationInstance?.GetType().FullName?.Contains("SqlServer") ?? false))
                .ToList();
            foreach (var descriptor in sqlServerDescriptors)
            {
                services.Remove(descriptor);
            }

            var optionsConfigDescriptors = services
                .Where(d => d.ServiceType.IsGenericType
                            && d.ServiceType.GetGenericTypeDefinition().FullName ==
                            "Microsoft.EntityFrameworkCore.Infrastructure.IDbContextOptionsConfiguration`1")
                .ToList();
            foreach (var descriptor in optionsConfigDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
                options.UseInMemoryDatabase(_databaseName)
                    .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });

            services.Configure<AuthenticationSchemeOptions>(options =>
            {
                options.TimeProvider = TimeProvider.System;
            });

            var serviceProvider = services.BuildServiceProvider();
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
    }
}
