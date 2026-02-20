using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class RevenueDashboardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RevenueDashboardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FinancialOverview_CalculatesTotalRevenue()
    {
        var (admin, members) = await SeedAdminAndMembersAsync("total", 3);
        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");

        var planA = await CreatePlanAsync(client, "RD-A-total", 500);
        var planB = await CreatePlanAsync(client, "RD-B-total", 800);

        await CreateDirectMembershipAsync(client, members[0].Id, planA.Id, 500);
        await CreateDirectMembershipAsync(client, members[1].Id, planA.Id, 500);
        await CreateDirectMembershipAsync(client, members[2].Id, planB.Id, 800);

        var response = await client.GetAsync("/api/admin/dashboard/financial-overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<FinancialOverviewDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Data!.TotalRevenue >= 1800m);
        Assert.True(payload.Data.MembershipRevenue >= 1800m);
    }

    [Fact]
    public async Task FinancialOverview_CalculatesWalletCashIn()
    {
        var (admin, members) = await SeedAdminAndMembersAsync("cashin", 1);
        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");

        var topup = await client.PostAsJsonAsync("/api/wallet/topup", new AdminWalletTopUpDto
        {
            MemberId = members[0].Id,
            Amount = 250,
            Notes = "cash desk"
        });
        Assert.Equal(HttpStatusCode.OK, topup.StatusCode);

        var response = await client.GetAsync("/api/admin/dashboard/financial-overview");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<FinancialOverviewDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Data!.WalletCashIn >= 250m);
    }

    [Fact]
    public async Task TopPlans_AreOrderedByActivationCountThenRevenue()
    {
        var (admin, members) = await SeedAdminAndMembersAsync("topplans", 3);
        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");

        var planA = await CreatePlanAsync(client, "RD-A-topplans", 500);
        var planB = await CreatePlanAsync(client, "RD-B-topplans", 900);

        await CreateDirectMembershipAsync(client, members[0].Id, planA.Id, 500);
        await CreateDirectMembershipAsync(client, members[1].Id, planA.Id, 500);
        await CreateDirectMembershipAsync(client, members[2].Id, planB.Id, 900);

        var response = await client.GetAsync("/api/admin/dashboard/top-plans?top=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<TopSellingMembershipPlanDto>>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.NotNull(payload!.Data);
        Assert.True(payload.Data!.Count >= 2);
        var topA = payload.Data.FirstOrDefault(x => x.PlanId == planA.Id);
        var topB = payload.Data.FirstOrDefault(x => x.PlanId == planB.Id);
        Assert.NotNull(topA);
        Assert.NotNull(topB);
        Assert.True(topA!.ActivationCount >= topB!.ActivationCount);
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

    private static async Task CreateDirectMembershipAsync(HttpClient client, string memberId, int planId, decimal amount)
    {
        var response = await client.PostAsJsonAsync("/api/memberships/direct-create", new CreateDirectMembershipDto
        {
            MemberId = memberId,
            MembershipPlanId = planId,
            PaymentAmount = amount
        });
        response.EnsureSuccessStatusCode();
    }

    private async Task<(ApplicationUser Admin, List<Member> Members)> SeedAdminAndMembersAsync(string suffix, int memberCount)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, $"admin-rd-{suffix}@test.local");
        var members = new List<Member>();
        for (var i = 0; i < memberCount; i++)
        {
            members.Add(await CreateMemberAsync(userManager, $"member-rd-{suffix}-{i}@test.local"));
        }

        return (admin, members);
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
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Admin", LastName = "Revenue", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Admin");
        return user;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;
        var user = new Member { UserName = email, Email = email, FirstName = "Member", LastName = "Revenue", MemberCode = $"M{Random.Shared.Next(1000, 9999)}", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
