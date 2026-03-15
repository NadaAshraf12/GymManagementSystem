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

public class MembershipLifecycleExplicitFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MembershipLifecycleExplicitFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MemberSubscribe_Pending_AdminConfirm_ActivatesAndGeneratesCommission()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, trainer, member) = await SeedUsersAsync(suffix);
        await SeedAssignmentAsync(trainer.Id, member.Id);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"Plan-{suffix}", 100);

        SetTestAuth(client, member.Id, "Member");
        var subscribe = await client.PostAsJsonAsync("/api/memberships/subscribe", new RequestSubscriptionDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 100
        });
        Assert.Equal(HttpStatusCode.Created, subscribe.StatusCode);
        var subscribePayload = await subscribe.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(subscribePayload);
        Assert.Equal(MembershipStatus.PendingPayment, subscribePayload!.Data!.Status);

        SetTestAuth(client, admin.Id, "Admin");
        var confirm = await client.PostAsync($"/api/memberships/{subscribePayload.Data.Id}/confirm", null);
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Commissions.AnyAsync(c =>
            c.MembershipId == subscribePayload.Data.Id &&
            c.Source == CommissionSource.Activation));
    }

    [Fact]
    public async Task AdminDirectCreate_ImmediateActive_GeneratesCommission()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, trainer, member) = await SeedUsersAsync(suffix);
        await SeedAssignmentAsync(trainer.Id, member.Id);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"Direct-{suffix}", 120);

        var create = await client.PostAsJsonAsync("/api/memberships/direct-create", new CreateDirectMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 120
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var payload = await create.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(MembershipStatus.Active, payload!.Data!.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.Commissions.AnyAsync(c =>
            c.MembershipId == payload.Data!.Id &&
            c.Source == CommissionSource.Activation));
    }

    [Fact]
    public async Task SecondOpenMembershipAttempt_IsRejected()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, _, member) = await SeedUsersAsync(suffix);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"Single-{suffix}", 90);

        var first = await client.PostAsJsonAsync("/api/memberships/direct-create", new CreateDirectMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 90
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await client.PostAsJsonAsync("/api/memberships/direct-create", new CreateDirectMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 90
        });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task Upgrade_DoesNotGenerateCommission()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, trainer, member) = await SeedUsersAsync(suffix);
        await SeedAssignmentAsync(trainer.Id, member.Id);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var basic = await CreatePlanAsync(client, $"Basic-{suffix}", 100);
        var premium = await CreatePlanAsync(client, $"Premium-{suffix}", 180);
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 200 });

        var direct = await client.PostAsJsonAsync("/api/memberships/direct-create", new CreateDirectMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = basic.Id,
            PaymentAmount = 100
        });
        var directPayload = await direct.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(directPayload);

        SetTestAuth(client, member.Id, "Member");
        var upgrade = await client.PostAsJsonAsync("/api/memberships/upgrade", new UpgradeMembershipDto
        {
            MemberId = member.Id,
            NewMembershipPlanId = premium.Id
        });
        Assert.Equal(HttpStatusCode.OK, upgrade.StatusCode);
        var upgradePayload = await upgrade.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(upgradePayload);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.Commissions.AnyAsync(c =>
            c.MembershipId == upgradePayload!.Data!.Id &&
            c.Source == CommissionSource.Activation));
    }

    [Fact]
    public async Task Renewal_GeneratesRenewalCommission()
    {
        var seed = await SeedRenewalSeedAsync();
        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISubscriptionAutomationService>();
            await service.ProcessExpirationsAsync(DateTime.UtcNow);
        }

        using var verifyScope = _factory.Services.CreateScope();
        var db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var renewal = await db.Memberships
            .Where(m => m.MemberId == seed.MemberId && m.Id != seed.ExpiredMembershipId)
            .OrderByDescending(m => m.Id)
            .FirstOrDefaultAsync();
        Assert.NotNull(renewal);
        Assert.True(await db.Commissions.AnyAsync(c =>
            c.MembershipId == renewal!.Id &&
            c.Source == CommissionSource.Renewal));
    }

    [Fact]
    public async Task ActivationAndDirectCreate_AreAdminOnly()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, _, member) = await SeedUsersAsync(suffix);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"Auth-{suffix}", 100);
        var subscribe = await client.PostAsJsonAsync("/api/memberships/subscribe", new RequestSubscriptionDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 100
        });
        var payload = await subscribe.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(payload);

        SetTestAuth(client, member.Id, "Member");
        var confirmByMember = await client.PostAsync($"/api/memberships/{payload!.Data!.Id}/confirm", null);
        Assert.True(confirmByMember.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);

        var directByMember = await client.PostAsJsonAsync("/api/memberships/direct-create", new CreateDirectMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 100
        });
        Assert.True(directByMember.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
    }

    private async Task<(string MemberId, int ExpiredMembershipId)> SeedRenewalSeedAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(roleManager, "Member");
        await EnsureRoleAsync(roleManager, "Trainer");

        var member = await CreateMemberAsync(userManager, $"member-ren-{suffix}@test.local");
        var trainer = await CreateTrainerAsync(userManager, $"trainer-ren-{suffix}@test.local");
        await SeedAssignmentAsync(trainer.Id, member.Id);

        var plan = new MembershipPlan
        {
            Name = $"Renewal-{suffix}",
            DurationInDays = 30,
            Price = 100,
            IsActive = true
        };
        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync();

        var expiring = new Membership
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            StartDate = DateTime.UtcNow.AddDays(-40),
            EndDate = DateTime.UtcNow.AddDays(-2),
            Status = MembershipStatus.Active,
            AutoRenewEnabled = true
        };
        db.Memberships.Add(expiring);
        db.WalletTransactions.Add(new WalletTransaction
        {
            MemberId = member.Id,
            Amount = 120,
            Type = WalletTransactionType.ManualAdjustment,
            Description = "seed",
            CreatedByUserId = null
        });
        await db.SaveChangesAsync();

        return (member.Id, expiring.Id);
    }

    private async Task SeedAssignmentAsync(string trainerId, string memberId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (!await db.TrainerMemberAssignments.AnyAsync(x => x.TrainerId == trainerId && x.MemberId == memberId))
        {
            db.TrainerMemberAssignments.Add(new TrainerMemberAssignment
            {
                TrainerId = trainerId,
                MemberId = memberId,
                Notes = "lifecycle"
            });
            await db.SaveChangesAsync();
        }
    }

    private async Task<(ApplicationUser Admin, Trainer Trainer, Member Member)> SeedUsersAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Trainer");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, $"admin-life-{suffix}@test.local");
        var trainer = await CreateTrainerAsync(userManager, $"trainer-life-{suffix}@test.local");
        var member = await CreateMemberAsync(userManager, $"member-life-{suffix}@test.local");
        return (admin, trainer, member);
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
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Admin", LastName = "User", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Admin");
        return user;
    }

    private static async Task<Trainer> CreateTrainerAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Trainer;
        if (existing != null) return existing;
        var user = new Trainer { UserName = email, Email = email, FirstName = "T", LastName = "T", Specialty = "S", Certification = "C", Experience = "E", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Trainer@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Trainer");
        return user;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;
        var user = new Member { UserName = email, Email = email, FirstName = "M", LastName = "M", MemberCode = $"M{Random.Shared.Next(1000, 9999)}", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
