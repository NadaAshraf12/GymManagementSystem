using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class MembershipFreezeFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MembershipFreezeFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task FreezeThenResume_PausesExpirationAndPreventsAutoRenewCharge()
    {
        var (admin, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var planCreate = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = "Freeze Plan",
            DurationInDays = 10,
            Price = 50,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, planCreate.StatusCode);

        var plan = await planCreate.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions);
        Assert.NotNull(plan);

        var create = await client.PostAsJsonAsync("/api/memberships/subscribe/ingym", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan!.Data!.Id,
            StartDate = DateTime.UtcNow.Date.AddDays(-8),
            PaymentAmount = 50,
            WalletAmountToUse = 0,
            AutoRenewEnabled = true
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var created = await create.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(created);
        var membershipId = created!.Data!.Id;

        var freezeStart = DateTime.UtcNow.AddDays(-2);
        var freeze = await client.PostAsJsonAsync($"/api/memberships/{membershipId}/freeze", new FreezeMembershipDto
        {
            FreezeStartDate = freezeStart
        });
        Assert.Equal(HttpStatusCode.OK, freeze.StatusCode);

        using (var scope = _factory.Services.CreateScope())
        {
            var automation = scope.ServiceProvider.GetRequiredService<ISubscriptionAutomationService>();
            await automation.ProcessExpirationsAsync(DateTime.UtcNow.AddDays(5));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var frozenMembership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId);
            Assert.NotNull(frozenMembership);
            Assert.Equal(MembershipStatus.Frozen, frozenMembership!.Status);

            var renewalDebits = await db.WalletTransactions
                .Where(w => w.MemberId == member.Id && w.Type == WalletTransactionType.MembershipRenewal)
                .ToListAsync();
            Assert.Empty(renewalDebits);
        }

        var beforeResume = await GetMembershipEndDateAsync(membershipId);

        var resume = await client.PostAsync($"/api/memberships/{membershipId}/resume", null);
        Assert.Equal(HttpStatusCode.OK, resume.StatusCode);

        var afterResume = await GetMembershipEndDateAsync(membershipId);
        Assert.True(afterResume > beforeResume);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var resumed = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId);
            Assert.NotNull(resumed);
            Assert.Equal(MembershipStatus.Active, resumed!.Status);
            Assert.NotNull(resumed.FreezeStartDate);
            Assert.NotNull(resumed.FreezeEndDate);
        }
    }

    private async Task<DateTime> GetMembershipEndDateAsync(int membershipId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId);
        return membership!.EndDate;
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, "admin-freeze@test.local");
        var member = await CreateMemberAsync(userManager, "member-freeze@test.local");

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
        if (existing != null)
        {
            return existing;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Freeze",
            LastName = "Admin",
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(admin, "Admin");
        return admin;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null)
        {
            return existing;
        }

        var member = new Member
        {
            UserName = email,
            Email = email,
            FirstName = "Freeze",
            LastName = "Member",
            MemberCode = "M7001",
            Gender = "M",
            Address = string.Empty,
            EmergencyContact = string.Empty,
            MedicalConditions = string.Empty,
            IsActive = true,
            JoinDate = DateTime.UtcNow,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(member, "Member@123");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

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
