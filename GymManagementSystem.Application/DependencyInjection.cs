using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Application.Services;
using GymManagementSystem.Application.Configrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mapster;
using GymManagementSystem.Application.Mappings;
using FluentValidation;
using System.Reflection;

namespace GymManagementSystem.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        // Register Services
        services.AddScoped<IMemberService, MemberService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ISessionService, SessionService>();
        services.AddScoped<ITrainerAssignmentService, TrainerAssignmentService>();
        services.AddScoped<ITrainerService, TrainerService>();
        services.AddScoped<ITrainingPlanService, TrainingPlanService>();
        services.AddScoped<INutritionPlanService, NutritionPlanService>();
        services.AddScoped<IAdminService, AdminService>();

        services.Configure<JwtSettings>(options =>
        {
            options.Key = configuration["Jwt:Key"] ?? string.Empty;
            options.Issuer = configuration["Jwt:Issuer"] ?? string.Empty;
            options.Audience = configuration["Jwt:Audience"] ?? string.Empty;
            options.ExpireMinutes = int.Parse(configuration["Jwt:ExpireMinutes"] ?? "60");
        });

        MapsterConfig.Register();
        var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
        services.AddSingleton(typeAdapterConfig);

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        return services;
    }
}