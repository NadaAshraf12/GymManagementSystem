using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.WebUI.Seeding;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Ensure roles
        var roles = new[] { "Admin", "Trainer", "Member", "Receptionist" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Ensure admin user
        const string adminEmail = "admin@gmail.com";
        const string adminPassword = "Admin@123";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "System",
                LastName = "Admin",
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new Exception($"Failed to create admin user: {errors}");
            }
        }

        // Ensure admin role assignment
        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        // Ensure trainer user
        const string trainerEmail = "trainer@gmail.com";
        var trainer = await userManager.FindByEmailAsync(trainerEmail) as Trainer;
        if (trainer == null)
        {
            trainer = new Trainer
            {
                UserName = trainerEmail,
                Email = trainerEmail,
                FirstName = "Demo",
                LastName = "Trainer",
                Specialty = "General Fitness",
                Certification = "CPT",
                Experience = "3 years",
                IsActive = true,
                HireDate = DateTime.UtcNow
            };
            var created = await userManager.CreateAsync(trainer, "Trainer@123");
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(trainer, "Trainer");
            }
        }

        const string memberEmail = "member@gmail.com";
        var member = await userManager.FindByEmailAsync(memberEmail) as Member;
        if (member == null)
        {
            member = new Member
            {
                UserName = memberEmail,
                Email = memberEmail,
                FirstName = "Demo",
                LastName = "Member",
                MemberCode = "M0001",
                Gender = "M",
                Address = "",
                EmergencyContact = "",
                MedicalConditions = "",
                IsActive = true,
                JoinDate = DateTime.UtcNow
            };
            var created = await userManager.CreateAsync(member, "Member@123");
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(member, "Member");
            }
        }

        // Ensure default active membership plans for demos
        var hasPlans = await dbContext.MembershipPlans
            .AsNoTracking()
            .AnyAsync();

        if (!hasPlans)
        {
            dbContext.MembershipPlans.AddRange(
                new MembershipPlan
                {
                    Name = "Monthly Basic",
                    Description = "Monthly basic plan.",
                    DurationInDays = 30,
                    Price = 500m,
                    CommissionRate = 10m,
                    IncludedSessionsPerMonth = 4,
                    SessionDiscountPercentage = 0m,
                    PriorityBooking = false,
                    AddOnAccess = true,
                    IsActive = true,
                    IsDeleted = false
                },
                new MembershipPlan
                {
                    Name = "Monthly Premium",
                    Description = "Monthly premium plan.",
                    DurationInDays = 30,
                    Price = 800m,
                    CommissionRate = 15m,
                    IncludedSessionsPerMonth = 8,
                    SessionDiscountPercentage = 10m,
                    PriorityBooking = true,
                    AddOnAccess = true,
                    IsActive = true,
                    IsDeleted = false
                },
                new MembershipPlan
                {
                    Name = "Yearly Premium",
                    Description = "Yearly premium plan.",
                    DurationInDays = 365,
                    Price = 7000m,
                    CommissionRate = 20m,
                    IncludedSessionsPerMonth = 10,
                    SessionDiscountPercentage = 15m,
                    PriorityBooking = true,
                    AddOnAccess = true,
                    IsActive = true,
                    IsDeleted = false
                });

            await dbContext.SaveChangesAsync();
        }
    }
}


