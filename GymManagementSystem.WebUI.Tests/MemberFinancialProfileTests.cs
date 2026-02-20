using System.Net.Http.Json;
using System.Text.Json;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class MemberFinancialProfileTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MemberFinancialProfileTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FinancialProfile_ReturnsWalletLedger_Purchases_MembershipHistory()
    {
        var (admin, member) = await SeedAdminAndMemberAsync("fp");
        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");

        var plan = await CreatePlanAsync(client, "FP-Plan", 500);
        await client.PostAsJsonAsync("/api/memberships/direct-create", new CreateDirectMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 500
        });

        await client.PostAsJsonAsync("/api/wallet/topup", new AdminWalletTopUpDto
        {
            MemberId = member.Id,
            Amount = 300,
            Notes = "cash"
        });

        var addOn = await CreateAddOnAsync(client, "FP AddOn", 50);
        SetTestAuth(client, member.Id, "Member");
        await client.PostAsJsonAsync("/api/addons/purchase", new PurchaseAddOnDto
        {
            MemberId = member.Id,
            AddOnId = addOn.Id
        });

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IMemberPlansService>();
        var profile = await service.GetMemberFinancialProfileAsync(member.Id);

        Assert.True(profile.WalletTransactions.Count > 0);
        Assert.True(profile.Purchases.Count > 0);
        Assert.True(profile.MembershipHistory.Count > 0);
    }

    private async Task<MembershipPlanReadDto> CreatePlanAsync(HttpClient client, string name, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = name,
            DurationInDays = 30,
            Price = price,
            IsActive = true
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions);
        return payload!.Data!;
    }

    private async Task<AddOnReadDto> CreateAddOnAsync(HttpClient client, string name, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/addons", new CreateAddOnDto
        {
            Name = name,
            Price = price
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<AddOnReadDto>>(JsonOptions);
        return payload!.Data!;
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedAdminAndMemberAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, $"admin-fp-{suffix}@test.local");
        var member = await CreateMemberAsync(userManager, $"member-fp-{suffix}@test.local");
        return (admin, member);
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
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Admin", LastName = "FP", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Admin");
        return user;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;
        var user = new Member { UserName = email, Email = email, FirstName = "Member", LastName = "FP", MemberCode = $"M{Random.Shared.Next(1000, 9999)}", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
