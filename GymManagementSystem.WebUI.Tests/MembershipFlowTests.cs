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

public class MembershipFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MembershipFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MemberOnlineSubscription_AdminConfirms_ActivatesMembershipAndUpdatesWalletAndAudit()
    {
        var (admin, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var createPlanDto = new CreateMembershipPlanDto
        {
            Name = "Monthly",
            DurationInDays = 30,
            Price = 100,
            Description = "Monthly membership",
            IsActive = true
        };

        var createPlanResponse = await client.PostAsJsonAsync("/api/membershipplans", createPlanDto);
        Assert.Equal(HttpStatusCode.Created, createPlanResponse.StatusCode);

        var createdPlan = await createPlanResponse.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions);
        Assert.NotNull(createdPlan);
        Assert.True(createdPlan!.Success);

        SetTestAuth(client, member.Id, "Member");
        var subscribeDto = new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = createdPlan.Data!.Id,
            StartDate = DateTime.UtcNow.Date,
            PaymentAmount = 130,
            WalletAmountToUse = 0
        };

        var subscribeResponse = await client.PostAsJsonAsync("/api/memberships/subscribe/online", subscribeDto);
        Assert.Equal(HttpStatusCode.Created, subscribeResponse.StatusCode);

        var subscribed = await subscribeResponse.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(subscribed);
        Assert.True(subscribed!.Success);
        Assert.Equal(MembershipStatus.PendingPayment, subscribed.Data!.Status);
        Assert.NotEmpty(subscribed.Data.Payments);
        Assert.Equal(PaymentStatus.Pending, subscribed.Data.Payments[0].PaymentStatus);

        var paymentId = subscribed.Data.Payments[0].Id;

        SetTestAuth(client, admin.Id, "Admin");
        var confirmResponse = await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/confirm", new ConfirmPaymentDto());
        Assert.Equal(HttpStatusCode.OK, confirmResponse.StatusCode);

        var confirmed = await confirmResponse.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(confirmed);
        Assert.True(confirmed!.Success);
        Assert.Equal(MembershipStatus.Active, confirmed.Data!.Status);
        Assert.Equal(PaymentStatus.Confirmed, confirmed.Data.Payments.Single().PaymentStatus);

        await AssertWalletAndAuditAsync(member.Id, confirmed.Data.Id, paymentId, admin.Id);
    }

    private async Task AssertWalletAndAuditAsync(string memberId, int membershipId, int paymentId, string adminId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var member = await db.Members.FirstOrDefaultAsync(m => m.Id == memberId);
        Assert.NotNull(member);
        Assert.Equal(30, member!.WalletBalance);

        var membershipAuditLogs = await db.AuditLogs
            .Where(a => a.EntityName == nameof(Membership) && a.EntityId == membershipId.ToString())
            .ToListAsync();

        var paymentAuditLogs = await db.AuditLogs
            .Where(a => a.EntityName == nameof(Payment) && a.EntityId == paymentId.ToString())
            .ToListAsync();

        Assert.NotEmpty(membershipAuditLogs);
        Assert.NotEmpty(paymentAuditLogs);

        var membershipModifiedLog = membershipAuditLogs.SingleOrDefault(a => a.Action == "Modified");
        Assert.NotNull(membershipModifiedLog);
        Assert.Equal(adminId, membershipModifiedLog!.UserId);
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, "admin-membership@test.local");
        var member = await CreateMemberAsync(userManager, "member-membership@test.local");

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
            FirstName = "System",
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
            FirstName = "Test",
            LastName = "Member",
            MemberCode = "M3001",
            Gender = "M",
            Address = string.Empty,
            EmergencyContact = string.Empty,
            MedicalConditions = string.Empty,
            IsActive = true,
            JoinDate = DateTime.UtcNow,
            WalletBalance = 0,
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
