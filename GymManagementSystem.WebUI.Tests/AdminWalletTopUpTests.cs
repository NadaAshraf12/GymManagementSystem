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

public class AdminWalletTopUpTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AdminWalletTopUpTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminTopUp_IncreasesWalletBalance()
    {
        var (admin, member) = await SeedAdminAndMemberAsync("balance");
        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");

        var before = await client.GetAsync($"/api/wallet/{member.Id}");
        var beforePayload = await before.Content.ReadFromJsonAsync<ApiResponse<WalletBalanceDto>>(JsonOptions);
        var beforeBalance = beforePayload!.Data!.WalletBalance;

        var topup = await client.PostAsJsonAsync("/api/wallet/topup", new AdminWalletTopUpDto
        {
            MemberId = member.Id,
            Amount = 75,
            Notes = "cash at desk"
        });
        Assert.Equal(HttpStatusCode.OK, topup.StatusCode);

        var after = await client.GetAsync($"/api/wallet/{member.Id}");
        var afterPayload = await after.Content.ReadFromJsonAsync<ApiResponse<WalletBalanceDto>>(JsonOptions);
        Assert.Equal(beforeBalance + 75, afterPayload!.Data!.WalletBalance);
    }

    [Fact]
    public async Task AdminTopUp_CreatesLedgerEntry()
    {
        var (admin, member) = await SeedAdminAndMemberAsync("ledger");
        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");

        var response = await client.PostAsJsonAsync("/api/wallet/topup", new AdminWalletTopUpDto
        {
            MemberId = member.Id,
            Amount = 40,
            Notes = "frontdesk"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.WalletTransactions.AnyAsync(t =>
            t.MemberId == member.Id &&
            t.Type == WalletTransactionType.Credit &&
            t.Amount == 40));
    }

    [Fact]
    public async Task WalletTopUp_UnauthorizedUser_IsBlocked()
    {
        var (_, member) = await SeedAdminAndMemberAsync("auth");
        var client = _factory.CreateClient();
        SetTestAuth(client, member.Id, "Member");

        var response = await client.PostAsJsonAsync("/api/wallet/topup", new AdminWalletTopUpDto
        {
            MemberId = member.Id,
            Amount = 10
        });

        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WalletTopUp_NegativeAmount_IsRejected()
    {
        var (admin, member) = await SeedAdminAndMemberAsync("negative");
        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");

        var response = await client.PostAsJsonAsync("/api/wallet/topup", new AdminWalletTopUpDto
        {
            MemberId = member.Id,
            Amount = -10
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedAdminAndMemberAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, $"admin-topup-{suffix}@test.local");
        var member = await CreateMemberAsync(userManager, $"member-topup-{suffix}@test.local");
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

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Admin",
            LastName = "TopUp",
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Admin");
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
            LastName = "TopUp",
            MemberCode = $"M{Random.Shared.Next(1000, 9999)}",
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
