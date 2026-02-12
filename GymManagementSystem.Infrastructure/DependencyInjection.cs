using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Infrastructure.Data;
using GymManagementSystem.Infrastructure.Repositories;
using GymManagementSystem.Infrastructure.Services;

namespace GymManagementSystem.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<AuditSaveChangesInterceptor>();

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));

            services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IChatRepository, ChatRepository>();
            services.AddScoped<IInvoicePdfGenerator, InvoicePdfGenerator>();

            return services;
        }
    }
}
