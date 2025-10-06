using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace GymManagementSystem.WebUI.Seeding;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

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

        // Optionally seed a demo trainer and member for quick start
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
    }
}


