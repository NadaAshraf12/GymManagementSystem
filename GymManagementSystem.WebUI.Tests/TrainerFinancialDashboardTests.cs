using System.Net;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class TrainerFinancialDashboardTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public TrainerFinancialDashboardTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TrainerSeesOnlyHisData()
    {
        var seed = await SeedFinancialDataAsync("isolation");

        using var scope = _factory.Services.CreateScope();
        var trainerService = scope.ServiceProvider.GetRequiredService<ITrainerService>();

        var profile = await trainerService.GetTrainerFinancialProfileAsync(seed.Trainer1.Id);

        Assert.All(profile.Commissions, row => Assert.DoesNotContain(seed.MemberB.FirstName, row.MemberName));
        Assert.All(profile.MembershipRevenues, row => Assert.DoesNotContain(seed.MemberB.FirstName, row.MemberName));
        Assert.All(profile.SessionEarnings, row => Assert.DoesNotContain("Trainer2", row.SessionTitle));
    }

    [Fact]
    public async Task GeneratedVsPaidCommission_AreSeparated()
    {
        var seed = await SeedFinancialDataAsync("commission-split");

        using var scope = _factory.Services.CreateScope();
        var trainerService = scope.ServiceProvider.GetRequiredService<ITrainerService>();

        var profile = await trainerService.GetTrainerFinancialProfileAsync(seed.Trainer1.Id);

        Assert.Equal(250m, profile.TotalGeneratedCommission);
        Assert.Equal(150m, profile.TotalPaidCommission);
        Assert.Equal(100m, profile.TotalPendingCommission);
    }

    [Fact]
    public async Task MembershipRevenue_IsCalculatedForTrainerMembers()
    {
        var seed = await SeedFinancialDataAsync("membership-revenue");

        using var scope = _factory.Services.CreateScope();
        var trainerService = scope.ServiceProvider.GetRequiredService<ITrainerService>();

        var profile = await trainerService.GetTrainerFinancialProfileAsync(seed.Trainer1.Id);

        Assert.Equal(1200m, profile.MembershipRevenueFromTrainerMembers);
        Assert.Equal(2, profile.MembershipRevenues.Count);
    }

    [Fact]
    public async Task SessionRevenue_IsCalculatedFromPaidSessions()
    {
        var seed = await SeedFinancialDataAsync("session-revenue");

        using var scope = _factory.Services.CreateScope();
        var trainerService = scope.ServiceProvider.GetRequiredService<ITrainerService>();

        var profile = await trainerService.GetTrainerFinancialProfileAsync(seed.Trainer1.Id);

        Assert.Equal(120m, profile.SessionRevenue);
        Assert.Single(profile.SessionEarnings);
    }

    [Fact]
    public async Task UnauthorizedUser_IsBlockedFromFinancialDashboard()
    {
        var seed = await SeedFinancialDataAsync("unauthorized");
        var client = _factory.CreateClient();
        SetTestAuth(client, seed.MemberA.Id, "Member");

        var response = await client.GetAsync("/Trainer/FinancialDashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<TrainerFinancialSeed> SeedFinancialDataAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(roleManager, "Trainer");
        await EnsureRoleAsync(roleManager, "Member");

        var branch = new Branch
        {
            Name = $"Branch-{suffix}-{Guid.NewGuid():N}",
            Address = "Address"
        };
        db.Branches.Add(branch);
        await db.SaveChangesAsync();

        var trainer1 = await CreateTrainerAsync(userManager, $"trainer1-{suffix}-{Guid.NewGuid():N}@test.local", "Trainer1", branch.Id);
        var trainer2 = await CreateTrainerAsync(userManager, $"trainer2-{suffix}-{Guid.NewGuid():N}@test.local", "Trainer2", branch.Id);

        var memberA = await CreateMemberAsync(userManager, $"memberA-{suffix}-{Guid.NewGuid():N}@test.local", "MemberA", branch.Id);
        var memberB = await CreateMemberAsync(userManager, $"memberB-{suffix}-{Guid.NewGuid():N}@test.local", "MemberB", branch.Id);
        var memberC = await CreateMemberAsync(userManager, $"memberC-{suffix}-{Guid.NewGuid():N}@test.local", "MemberC", branch.Id);

        var plan = new MembershipPlan
        {
            Name = $"Plan-{suffix}-{Guid.NewGuid():N}",
            DurationInDays = 30,
            Price = 500,
            CommissionRate = 10,
            IsActive = true
        };
        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync();

        var assignment1 = new TrainerMemberAssignment { TrainerId = trainer1.Id, MemberId = memberA.Id };
        var assignment2 = new TrainerMemberAssignment { TrainerId = trainer1.Id, MemberId = memberC.Id };
        var assignment3 = new TrainerMemberAssignment { TrainerId = trainer2.Id, MemberId = memberB.Id };
        db.TrainerMemberAssignments.AddRange(assignment1, assignment2, assignment3);

        var membershipA = new Membership
        {
            MemberId = memberA.Id,
            MembershipPlanId = plan.Id,
            BranchId = branch.Id,
            StartDate = DateTime.UtcNow.Date.AddDays(-10),
            EndDate = DateTime.UtcNow.Date.AddDays(20),
            Status = MembershipStatus.Active,
            Source = MembershipSource.InGym,
            TotalPaid = 500
        };
        var membershipB = new Membership
        {
            MemberId = memberB.Id,
            MembershipPlanId = plan.Id,
            BranchId = branch.Id,
            StartDate = DateTime.UtcNow.Date.AddDays(-8),
            EndDate = DateTime.UtcNow.Date.AddDays(22),
            Status = MembershipStatus.Active,
            Source = MembershipSource.InGym,
            TotalPaid = 900
        };
        var membershipC = new Membership
        {
            MemberId = memberC.Id,
            MembershipPlanId = plan.Id,
            BranchId = branch.Id,
            StartDate = DateTime.UtcNow.Date.AddDays(-6),
            EndDate = DateTime.UtcNow.Date.AddDays(24),
            Status = MembershipStatus.Active,
            Source = MembershipSource.InGym,
            TotalPaid = 700
        };
        db.Memberships.AddRange(membershipA, membershipB, membershipC);
        await db.SaveChangesAsync();

        db.Commissions.AddRange(
            new Commission
            {
                TrainerId = trainer1.Id,
                MembershipId = membershipA.Id,
                BranchId = branch.Id,
                Source = CommissionSource.Activation,
                Percentage = 10,
                CalculatedAmount = 100,
                IsPaid = false
            },
            new Commission
            {
                TrainerId = trainer1.Id,
                MembershipId = membershipC.Id,
                BranchId = branch.Id,
                Source = CommissionSource.Activation,
                Percentage = 10,
                CalculatedAmount = 150,
                IsPaid = true,
                PaidAt = DateTime.UtcNow
            },
            new Commission
            {
                TrainerId = trainer2.Id,
                MembershipId = membershipB.Id,
                BranchId = branch.Id,
                Source = CommissionSource.Activation,
                Percentage = 10,
                CalculatedAmount = 200,
                IsPaid = true,
                PaidAt = DateTime.UtcNow
            });

        var sessionTrainer1 = new WorkoutSession
        {
            TrainerId = trainer1.Id,
            Title = "Trainer1 Session",
            Description = "Desc",
            SessionDate = DateTime.UtcNow.Date,
            BranchId = branch.Id,
            Price = 120,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            MaxParticipants = 10
        };
        var sessionTrainer2 = new WorkoutSession
        {
            TrainerId = trainer2.Id,
            Title = "Trainer2 Session",
            Description = "Desc",
            SessionDate = DateTime.UtcNow.Date,
            BranchId = branch.Id,
            Price = 300,
            StartTime = new TimeSpan(12, 0, 0),
            EndTime = new TimeSpan(13, 0, 0),
            MaxParticipants = 10
        };
        db.WorkoutSessions.AddRange(sessionTrainer1, sessionTrainer2);
        await db.SaveChangesAsync();

        db.MemberSessions.AddRange(
            new MemberSession
            {
                MemberId = memberA.Id,
                WorkoutSessionId = sessionTrainer1.Id,
                BookingDate = DateTime.UtcNow,
                OriginalPrice = 120,
                ChargedPrice = 120,
                AppliedDiscountPercentage = 0
            },
            new MemberSession
            {
                MemberId = memberB.Id,
                WorkoutSessionId = sessionTrainer2.Id,
                BookingDate = DateTime.UtcNow,
                OriginalPrice = 300,
                ChargedPrice = 300,
                AppliedDiscountPercentage = 0
            });

        await db.SaveChangesAsync();

        return new TrainerFinancialSeed
        {
            Trainer1 = trainer1,
            Trainer2 = trainer2,
            MemberA = memberA,
            MemberB = memberB
        };
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    private static async Task<Trainer> CreateTrainerAsync(UserManager<ApplicationUser> userManager, string email, string firstName, int? branchId)
    {
        var trainer = new Trainer
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = "Financial",
            Specialty = "General",
            Certification = "Cert",
            Experience = "3 years",
            Salary = 1000,
            BankAccount = "BANK",
            IsActive = true,
            EmailConfirmed = true,
            BranchId = branchId
        };

        var result = await userManager.CreateAsync(trainer, "Trainer@123");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(trainer, "Trainer");
        return trainer;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email, string firstName, int? branchId)
    {
        var member = new Member
        {
            UserName = email,
            Email = email,
            FirstName = firstName,
            LastName = "Financial",
            MemberCode = $"M{Random.Shared.Next(10000, 99999)}",
            Gender = "M",
            Address = "Address",
            EmergencyContact = "Emergency",
            MedicalConditions = "None",
            IsActive = true,
            EmailConfirmed = true,
            BranchId = branchId
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

    private sealed class TrainerFinancialSeed
    {
        public Trainer Trainer1 { get; init; } = null!;
        public Trainer Trainer2 { get; init; } = null!;
        public Member MemberA { get; init; } = null!;
        public Member MemberB { get; init; } = null!;
    }
}
