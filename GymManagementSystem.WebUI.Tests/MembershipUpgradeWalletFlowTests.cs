using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class MembershipUpgradeWalletFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MembershipUpgradeWalletFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MemberUpgradeUsingWallet_DebitsDifference_CreatesNewMembership_AndAudit()
    {
        var (admin, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var basicPlan = await CreatePlan(client, "Basic", 100);
        var premiumPlan = await CreatePlan(client, "Premium", 180);

        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto
        {
            MemberId = member.Id,
            Amount = 200
        });

        SetTestAuth(client, member.Id, "Member");
        var createMembership = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = basicPlan.Id,
            PaymentAmount = 100,
            WalletAmountToUse = 0
        });
        var createdPayload = await createMembership.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        var paymentId = createdPayload!.Data!.Payments.Single().Id;

        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/confirm", new ConfirmPaymentDto());

        SetTestAuth(client, member.Id, "Member");
        var upgrade = await client.PostAsJsonAsync("/api/memberships/upgrade", new UpgradeMembershipDto
        {
            MemberId = member.Id,
            NewMembershipPlanId = premiumPlan.Id
        });
        Assert.Equal(HttpStatusCode.OK, upgrade.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var memberships = await db.Memberships
            .Where(m => m.MemberId == member.Id)
            .OrderBy(m => m.Id)
            .ToListAsync();
        Assert.True(memberships.Count >= 2);
        Assert.Contains(memberships, m => m.MembershipPlanId == premiumPlan.Id && m.Status == MembershipStatus.Active);
        Assert.Contains(memberships, m => m.MembershipPlanId == basicPlan.Id && m.Status == MembershipStatus.Cancelled);

        Assert.True(await db.WalletTransactions.AnyAsync(t =>
            t.MemberId == member.Id &&
            t.Type == WalletTransactionType.MembershipUpgrade &&
            t.Amount == -80));

        Assert.True(await db.AuditLogs.AnyAsync(a => a.EntityName == nameof(Membership)));
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        var admin = await CreateAdminAsync(userManager, "admin-upgrade@test.local");
        var member = await CreateMemberAsync(userManager, "member-upgrade@test.local");
        return (admin, member);
    }

    private async Task<MembershipPlanReadDto> CreatePlan(HttpClient client, string name, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = name,
            DurationInDays = 30,
            Price = price,
            IsActive = true
        });
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions);
        return payload!.Data!;
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
        var member = new Member { UserName = email, Email = email, FirstName = "Member", LastName = "User", MemberCode = "M9601", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
