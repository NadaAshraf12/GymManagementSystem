using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class SubscriptionAutomationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SubscriptionAutomationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ExpiredMembership_IsAutoRenewed_WhenWalletIsSufficient()
    {
        var seed = await SeedDataAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var automationService = scope.ServiceProvider.GetRequiredService<ISubscriptionAutomationService>();
            var result = await automationService.ProcessExpirationsAsync(DateTime.UtcNow);

            Assert.True(result.ExpiredCount >= 1);
            Assert.True(result.AutoRenewedCount >= 1);
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var original = await db.Memberships.FirstOrDefaultAsync(m => m.Id == seed.MembershipId);
            Assert.NotNull(original);
            Assert.Equal(MembershipStatus.Expired, original!.Status);

            var renewed = await db.Memberships
                .Where(m => m.MemberId == seed.MemberId && m.Id != seed.MembershipId)
                .OrderByDescending(m => m.Id)
                .FirstOrDefaultAsync();

            Assert.NotNull(renewed);
            Assert.Equal(MembershipStatus.Active, renewed!.Status);
            Assert.Equal(seed.PlanId, renewed.MembershipPlanId);

            var renewalDebit = await db.WalletTransactions
                .Where(w => w.MemberId == seed.MemberId && w.Type == WalletTransactionType.MembershipRenewal)
                .OrderByDescending(w => w.Id)
                .FirstOrDefaultAsync();

            Assert.NotNull(renewalDebit);
            Assert.Equal(-seed.PlanPrice, renewalDebit!.Amount);
            Assert.Equal(renewed.Id, renewalDebit.ReferenceId);

            var computedBalance = await db.WalletTransactions
                .Where(w => w.MemberId == seed.MemberId)
                .SumAsync(w => w.Amount);
            Assert.Equal(seed.InitialWalletCredit - seed.PlanPrice, computedBalance);

            var membershipAudit = await db.AuditLogs
                .Where(a => a.EntityName == nameof(Membership)
                            && a.EntityId == seed.MembershipId.ToString()
                            && a.Action == "Modified")
                .AnyAsync();

            var walletAudit = await db.AuditLogs
                .Where(a => a.EntityName == nameof(WalletTransaction))
                .AnyAsync();

            Assert.True(membershipAudit);
            Assert.True(walletAudit);
        }
    }

    private async Task<(string MemberId, int MembershipId, int PlanId, decimal PlanPrice, decimal InitialWalletCredit)> SeedDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(roleManager, "Member");

        var member = await CreateMemberAsync(userManager, "member-automation@test.local");

        var plan = new MembershipPlan
        {
            Name = "Yearly",
            DurationInDays = 365,
            Price = 120,
            Description = "Auto-renew test plan",
            IsActive = true
        };
        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync();

        var membership = new Membership
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            StartDate = DateTime.UtcNow.AddDays(-40),
            EndDate = DateTime.UtcNow.AddDays(-5),
            Status = MembershipStatus.Active,
            Source = MembershipSource.Online,
            AutoRenewEnabled = true,
            TotalPaid = plan.Price,
            RemainingBalanceUsedFromWallet = 0
        };
        db.Memberships.Add(membership);

        db.WalletTransactions.Add(new WalletTransaction
        {
            MemberId = member.Id,
            Amount = 200,
            Type = WalletTransactionType.ManualAdjustment,
            ReferenceId = null,
            Description = "Initial wallet credit",
            CreatedByUserId = null
        });

        await db.SaveChangesAsync();

        return (member.Id, membership.Id, plan.Id, plan.Price, 200);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
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
            FirstName = "Automation",
            LastName = "Member",
            MemberCode = "M5001",
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
}

