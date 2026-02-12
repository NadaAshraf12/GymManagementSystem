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

public class BusinessRuleStabilizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BusinessRuleStabilizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PreventSecondOpenMembership_ForSameMember()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, member) = await SeedAdminMemberAsync(suffix);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"Monthly-{suffix}", 100);

        var first = await client.PostAsJsonAsync("/api/memberships/subscribe/ingym", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 100,
            WalletAmountToUse = 0
        });
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        SetTestAuth(client, member.Id, "Member");
        var second = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 100,
            WalletAmountToUse = 0
        });
        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task CommissionGeneratedOnce_ForActivation()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, trainer, member) = await SeedAdminTrainerMemberAsync(suffix);
        await SeedAssignmentAsync(trainer.Id, member.Id);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"Activation-{suffix}", 120);

        SetTestAuth(client, member.Id, "Member");
        var createMembership = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 120,
            WalletAmountToUse = 0
        });
        var payload = await createMembership.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(payload);
        var paymentId = payload!.Data!.Payments.Single().Id;

        SetTestAuth(client, admin.Id, "Admin");
        var confirm = await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/confirm", new ConfirmPaymentDto());
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var createdMembershipId = payload.Data.Id;
        var commissions = await db.Commissions
            .Where(c => c.MembershipId == createdMembershipId && c.Source == CommissionSource.Activation)
            .ToListAsync();
        Assert.Single(commissions);
    }

    [Fact]
    public async Task CommissionGeneratedOnce_ForRenewal()
    {
        var seed = await SeedRenewalDataAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<ISubscriptionAutomationService>();
            await service.ProcessExpirationsAsync(DateTime.UtcNow);
            await service.ProcessExpirationsAsync(DateTime.UtcNow.AddMinutes(1));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var renewal = await db.Memberships
                .Where(m => m.MemberId == seed.MemberId && m.Id != seed.ExpiredMembershipId)
                .OrderByDescending(m => m.Id)
                .FirstOrDefaultAsync();
            Assert.NotNull(renewal);

            var commissions = await db.Commissions
                .Where(c => c.MembershipId == renewal!.Id && c.Source == CommissionSource.Renewal)
                .ToListAsync();
            Assert.Single(commissions);
        }
    }

    [Fact]
    public async Task UpgradeCancelsOldMembership_AndCreatesOneActive()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, member) = await SeedAdminMemberAsync(suffix);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var basic = await CreatePlanAsync(client, $"Basic-{suffix}", 100);
        var premium = await CreatePlanAsync(client, $"Premium-{suffix}", 180);
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 200 });

        SetTestAuth(client, member.Id, "Member");
        var create = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = basic.Id,
            PaymentAmount = 100,
            WalletAmountToUse = 0
        });
        var createPayload = await create.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(createPayload);
        var paymentId = createPayload!.Data!.Payments.Single().Id;

        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/confirm", new ConfirmPaymentDto());

        SetTestAuth(client, member.Id, "Member");
        var upgrade = await client.PostAsJsonAsync("/api/memberships/upgrade", new UpgradeMembershipDto
        {
            MemberId = member.Id,
            NewMembershipPlanId = premium.Id
        });
        Assert.Equal(HttpStatusCode.OK, upgrade.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var active = await db.Memberships.CountAsync(m => m.MemberId == member.Id && m.Status == MembershipStatus.Active);
        Assert.Equal(1, active);
        Assert.True(await db.Memberships.AnyAsync(m => m.MemberId == member.Id && m.MembershipPlanId == basic.Id && m.Status == MembershipStatus.Cancelled));
        Assert.True(await db.Memberships.AnyAsync(m => m.MemberId == member.Id && m.MembershipPlanId == premium.Id && m.Status == MembershipStatus.Active));
    }

    [Fact]
    public async Task CommissionPayout_IsAdminOnly()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, trainer, member) = await SeedAdminTrainerMemberAsync(suffix);
        await SeedAssignmentAsync(trainer.Id, member.Id);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"Payout-{suffix}", 100);
        SetTestAuth(client, member.Id, "Member");
        var createMembership = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentAmount = 100
        });
        var createPayload = await createMembership.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(createPayload);

        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync($"/api/memberships/payments/{createPayload!.Data!.Payments.Single().Id}/confirm", new ConfirmPaymentDto());
        var unpaidResponse = await client.GetAsync("/api/commissions/unpaid");
        var unpaidPayload = await unpaidResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CommissionReadDto>>>(JsonOptions);
        Assert.NotNull(unpaidPayload);
        var commissionId = unpaidPayload!.Data!.Single().Id;

        SetTestAuth(client, member.Id, "Member");
        var markPaidByMember = await client.PostAsync($"/api/commissions/{commissionId}/mark-paid", null);
        Assert.True(markPaidByMember.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MembershipPlanManagement_IsAdminOnly()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (_, member) = await SeedAdminMemberAsync(suffix);
        var client = _factory.CreateClient();

        SetTestAuth(client, member.Id, "Member");
        var response = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = $"Unauthorized-{suffix}",
            DurationInDays = 30,
            Price = 50
        });

        Assert.True(response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized);
    }

    private async Task<(string MemberId, int ExpiredMembershipId)> SeedRenewalDataAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Member");
        await EnsureRoleAsync(roleManager, "Trainer");

        var member = await CreateMemberAsync(userManager, $"member-renew-{suffix}@test.local");
        var trainer = await CreateTrainerAsync(userManager, $"trainer-renew-{suffix}@test.local");
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

        var expiredCandidate = new Membership
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            BranchId = member.BranchId,
            StartDate = DateTime.UtcNow.AddDays(-40),
            EndDate = DateTime.UtcNow.AddDays(-1),
            Status = MembershipStatus.Active,
            Source = MembershipSource.Online,
            AutoRenewEnabled = true
        };
        db.Memberships.Add(expiredCandidate);
        db.WalletTransactions.Add(new WalletTransaction
        {
            MemberId = member.Id,
            Amount = 150,
            Type = WalletTransactionType.ManualAdjustment,
            Description = "seed",
            CreatedByUserId = null
        });
        await db.SaveChangesAsync();

        return (member.Id, expiredCandidate.Id);
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
                Notes = "stabilization"
            });
            await db.SaveChangesAsync();
        }
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedAdminMemberAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        var admin = await CreateAdminAsync(userManager, $"admin-stab-{suffix}@test.local");
        var member = await CreateMemberAsync(userManager, $"member-stab-{suffix}@test.local");
        return (admin, member);
    }

    private async Task<(ApplicationUser Admin, Trainer Trainer, Member Member)> SeedAdminTrainerMemberAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        await EnsureRoleAsync(roleManager, "Trainer");

        var admin = await CreateAdminAsync(userManager, $"admin-ctr-{suffix}@test.local");
        var trainer = await CreateTrainerAsync(userManager, $"trainer-ctr-{suffix}@test.local");
        var member = await CreateMemberAsync(userManager, $"member-ctr-{suffix}@test.local");
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
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "A", LastName = "A", IsActive = true, EmailConfirmed = true };
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
