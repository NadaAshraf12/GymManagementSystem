using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class RevenueMetricsAccuracyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public RevenueMetricsAccuracyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DashboardMetrics_IncludeSessionMembershipAddOnRevenue_AndWalletCirculation()
    {
        var (admin, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var planResponse = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = "Metrics Plan",
            DurationInDays = 30,
            Price = 100,
            IsActive = true
        });
        var plan = (await planResponse.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions))!.Data!;

        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 300 });

        SetTestAuth(client, member.Id, "Member");
        var sub = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 100
        });
        var subPayload = await sub.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        var paymentId = subPayload!.Data!.Payments.Single().Id;

        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/confirm", new ConfirmPaymentDto());

        var addOnResponse = await client.PostAsJsonAsync("/api/addons", new CreateAddOnDto { Name = "Metrics AddOn", Price = 30 });
        var addOn = (await addOnResponse.Content.ReadFromJsonAsync<ApiResponse<AddOnReadDto>>(JsonOptions))!.Data!;

        SetTestAuth(client, member.Id, "Member");
        await client.PostAsJsonAsync("/api/addons/purchase", new PurchaseAddOnDto { MemberId = member.Id, AddOnId = addOn.Id });

        SetTestAuth(client, admin.Id, "Admin");
        var metricsResponse = await client.GetAsync("/api/admin/dashboard/metrics");
        Assert.Equal(HttpStatusCode.OK, metricsResponse.StatusCode);
        var metrics = await metricsResponse.Content.ReadFromJsonAsync<ApiResponse<RevenueMetricsDto>>(JsonOptions);
        Assert.NotNull(metrics);
        Assert.True(metrics!.Data!.TotalMembershipRevenue >= 100);
        Assert.True(metrics.Data.TotalAddOnRevenue >= 30);
        Assert.True(metrics.Data.TotalRevenue >= metrics.Data.TotalMembershipRevenue + metrics.Data.TotalAddOnRevenue);
        Assert.True(metrics.Data.WalletTotalCredits >= metrics.Data.WalletTotalDebits);
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        var admin = await CreateAdminAsync(userManager, "admin-metrics@test.local");
        var member = await CreateMemberAsync(userManager, "member-metrics@test.local");
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
        var admin = new ApplicationUser { UserName = email, Email = email, FirstName = "Admin", LastName = "User", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(admin, "Admin");
        return admin;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;
        var member = new Member { UserName = email, Email = email, FirstName = "Member", LastName = "User", MemberCode = "M9801", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(member, "Member@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(member, "Member");
        return member;
    }

    private static void SetTestAuth(HttpClient client, string userId, string roles)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);
    }
}
