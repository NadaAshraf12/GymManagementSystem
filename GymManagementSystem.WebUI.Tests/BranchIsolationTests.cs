using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class BranchIsolationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BranchIsolationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateBranchAssignUsers_EnforcesBranchIsolationForTrainerWalletAccess()
    {
        var (admin, trainer, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var branchA = await CreateBranchAsync(client, "Branch A", "Addr A");
        var branchB = await CreateBranchAsync(client, "Branch B", "Addr B");

        var assignTrainer = await client.PostAsJsonAsync("/api/branches/assign-trainer", new AssignUserBranchDto
        {
            UserId = trainer.Id,
            BranchId = branchA.Id
        });
        Assert.Equal(HttpStatusCode.OK, assignTrainer.StatusCode);

        var assignMember = await client.PostAsJsonAsync("/api/branches/assign-member", new AssignUserBranchDto
        {
            UserId = member.Id,
            BranchId = branchB.Id
        });
        Assert.Equal(HttpStatusCode.OK, assignMember.StatusCode);

        SetTestAuth(client, trainer.Id, "Trainer");
        var walletResponse = await client.GetAsync($"/api/wallet/trainer/{member.Id}");
        Assert.Equal(HttpStatusCode.Unauthorized, walletResponse.StatusCode);
    }

    private async Task<BranchReadDto> CreateBranchAsync(HttpClient client, string name, string address)
    {
        var response = await client.PostAsJsonAsync("/api/branches", new CreateBranchDto
        {
            Name = name,
            Address = address,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<BranchReadDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotNull(payload!.Data);
        return payload.Data!;
    }

    private async Task<(ApplicationUser Admin, Trainer Trainer, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Trainer");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, "admin-branch@test.local");
        var trainer = await CreateTrainerAsync(userManager, "trainer-branch@test.local");
        var member = await CreateMemberAsync(userManager, "member-branch@test.local");
        return (admin, trainer, member);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task<ApplicationUser> CreateAdminAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null) return existing;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Admin",
            LastName = "Branch",
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Admin");
        return user;
    }

    private static async Task<Trainer> CreateTrainerAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Trainer;
        if (existing != null) return existing;

        var user = new Trainer
        {
            UserName = email,
            Email = email,
            FirstName = "Trainer",
            LastName = "Branch",
            Specialty = "General",
            Certification = "CPT",
            Experience = "3 years",
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, "Trainer@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Trainer");
        return user;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;

        var user = new Member
        {
            UserName = email,
            Email = email,
            FirstName = "Member",
            LastName = "Branch",
            MemberCode = "M9001",
            Gender = "M",
            Address = "A",
            EmergencyContact = "X",
            MedicalConditions = "None",
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(user, "Member@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Member");
        return user;
    }

    private static void SetTestAuth(HttpClient client, string userId, string roles)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);
    }
}
