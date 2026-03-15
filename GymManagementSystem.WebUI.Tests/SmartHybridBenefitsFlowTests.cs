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

public class SmartHybridBenefitsFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public SmartHybridBenefitsFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ActiveMembership_IncludedSession_AppliesFreeBooking()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, member, session) = await SeedUsersSessionAndMembershipAsync(
            suffix,
            sessionPrice: 50,
            includedSessionsPerMonth: 1,
            discountPercentage: 0,
            addOnAccess: true);

        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 20 });

        SetTestAuth(client, member.Id, "Member");
        var response = await client.PostAsJsonAsync("/api/sessions/book-paid", new PaidSessionBookingDto
        {
            MemberId = member.Id,
            WorkoutSessionId = session.Id
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SessionBookingResultDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.True(payload.Data!.Success);
        Assert.True(payload.Data.UsedIncludedSession);
        Assert.Equal(0, payload.Data.ChargedPrice);
        Assert.Equal(20, payload.Data.WalletBalance);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.MemberSessions.AnyAsync(ms =>
            ms.MemberId == member.Id &&
            ms.WorkoutSessionId == session.Id &&
            ms.UsedIncludedSession &&
            ms.ChargedPrice == 0));
        Assert.False(await db.WalletTransactions.AnyAsync(t =>
            t.MemberId == member.Id &&
            t.Type == WalletTransactionType.SessionBooking &&
            t.ReferenceId == session.Id));
    }

    [Fact]
    public async Task ActiveMembership_Discount_AppliesDiscountedWalletDebit()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, member, session) = await SeedUsersSessionAndMembershipAsync(
            suffix,
            sessionPrice: 80,
            includedSessionsPerMonth: 0,
            discountPercentage: 25,
            addOnAccess: true);

        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 100 });

        SetTestAuth(client, member.Id, "Member");
        var response = await client.PostAsJsonAsync("/api/sessions/book-paid", new PaidSessionBookingDto
        {
            MemberId = member.Id,
            WorkoutSessionId = session.Id
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SessionBookingResultDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.Equal(60, payload.Data!.ChargedPrice);
        Assert.Equal(25, payload.Data.DiscountPercentageApplied);
        Assert.Equal(40, payload.Data.WalletBalance);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.WalletTransactions.AnyAsync(t =>
            t.MemberId == member.Id &&
            t.Type == WalletTransactionType.SessionBooking &&
            t.ReferenceId == session.Id &&
            t.Amount == -60));
    }

    [Fact]
    public async Task RestrictedAddOn_WithoutActiveMembership_IsRejected()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, member) = await SeedUsersAsync(suffix);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 100 });

        var createAddOn = await client.PostAsJsonAsync("/api/addons", new CreateAddOnDto
        {
            Name = $"Restricted-{suffix}",
            Price = 30,
            RequiresActiveMembership = true
        });
        Assert.Equal(HttpStatusCode.Created, createAddOn.StatusCode);
        var addOnPayload = await createAddOn.Content.ReadFromJsonAsync<ApiResponse<AddOnReadDto>>(JsonOptions);
        Assert.NotNull(addOnPayload);

        SetTestAuth(client, member.Id, "Member");
        var purchase = await client.PostAsJsonAsync("/api/addons/purchase", new PurchaseAddOnDto
        {
            MemberId = member.Id,
            AddOnId = addOnPayload!.Data!.Id
        });

        Assert.Equal(HttpStatusCode.BadRequest, purchase.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.False(await db.WalletTransactions.AnyAsync(t =>
            t.MemberId == member.Id &&
            t.Type == WalletTransactionType.AddOnPurchase &&
            t.ReferenceId == addOnPayload.Data.Id));
    }

    [Fact]
    public async Task NoMembership_PaidSession_ChargesFullPrice()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, member, session) = await SeedUsersAndSessionAsync(suffix, sessionPrice: 30);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 100 });

        SetTestAuth(client, member.Id, "Member");
        var response = await client.PostAsJsonAsync("/api/sessions/book-paid", new PaidSessionBookingDto
        {
            MemberId = member.Id,
            WorkoutSessionId = session.Id
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SessionBookingResultDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(30, payload!.Data!.ChargedPrice);
        Assert.False(payload.Data.UsedIncludedSession);
        Assert.Equal(70, payload.Data.WalletBalance);
    }

    [Fact]
    public async Task IncludedSessions_ResetEachMonth_PreviousMonthUsageDoesNotConsumeCurrentQuota()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, member, session) = await SeedUsersSessionAndMembershipAsync(
            suffix,
            sessionPrice: 45,
            includedSessionsPerMonth: 1,
            discountPercentage: 0,
            addOnAccess: true);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var previousSession = new WorkoutSession
            {
                TrainerId = session.TrainerId,
                BranchId = session.BranchId,
                Title = $"PrevMonth-{suffix}",
                Description = "Previous month session",
                SessionDate = DateTime.UtcNow.Date.AddMonths(-1),
                StartTime = new TimeSpan(8, 0, 0),
                EndTime = new TimeSpan(9, 0, 0),
                MaxParticipants = 20,
                CurrentParticipants = 1,
                Price = 45
            };
            db.WorkoutSessions.Add(previousSession);
            await db.SaveChangesAsync();

            db.MemberSessions.Add(new MemberSession
            {
                MemberId = member.Id,
                WorkoutSessionId = previousSession.Id,
                BookingDate = DateTime.UtcNow.AddMonths(-1).Date.AddDays(1),
                Attended = true,
                OriginalPrice = 45,
                ChargedPrice = 0,
                AppliedDiscountPercentage = 0,
                UsedIncludedSession = true,
                PriorityBookingApplied = false
            });
            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 10 });

        SetTestAuth(client, member.Id, "Member");
        var response = await client.PostAsJsonAsync("/api/sessions/book-paid", new PaidSessionBookingDto
        {
            MemberId = member.Id,
            WorkoutSessionId = session.Id
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<SessionBookingResultDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Data!.UsedIncludedSession);
        Assert.Equal(0, payload.Data.ChargedPrice);
        Assert.Equal(10, payload.Data.WalletBalance);
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedUsersAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        await EnsureRoleAsync(roleManager, "Trainer");

        var admin = await CreateAdminAsync(userManager, $"admin-hybrid-{suffix}@test.local");
        var member = await CreateMemberAsync(userManager, $"member-hybrid-{suffix}@test.local");
        return (admin, member);
    }

    private async Task<(ApplicationUser Admin, Member Member, WorkoutSession Session)> SeedUsersAndSessionAsync(string suffix, decimal sessionPrice)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        await EnsureRoleAsync(roleManager, "Trainer");

        var admin = await CreateAdminAsync(userManager, $"admin-session-{suffix}@test.local");
        var trainer = await CreateTrainerAsync(userManager, $"trainer-session-{suffix}@test.local");
        var member = await CreateMemberAsync(userManager, $"member-session-{suffix}@test.local");

        var session = new WorkoutSession
        {
            TrainerId = trainer.Id,
            BranchId = member.BranchId,
            Title = $"Paid-{suffix}",
            Description = "Paid session",
            SessionDate = DateTime.UtcNow.Date.AddDays(2),
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            MaxParticipants = 20,
            CurrentParticipants = 0,
            Price = sessionPrice
        };
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync();
        return (admin, member, session);
    }

    private async Task<(ApplicationUser Admin, Member Member, WorkoutSession Session)> SeedUsersSessionAndMembershipAsync(
        string suffix,
        decimal sessionPrice,
        int includedSessionsPerMonth,
        decimal discountPercentage,
        bool addOnAccess)
    {
        var (admin, member, session) = await SeedUsersAndSessionAsync(suffix, sessionPrice);
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var plan = new MembershipPlan
        {
            Name = $"Plan-{suffix}",
            DurationInDays = 30,
            Price = 100,
            IncludedSessionsPerMonth = includedSessionsPerMonth,
            SessionDiscountPercentage = discountPercentage,
            PriorityBooking = false,
            AddOnAccess = addOnAccess,
            IsActive = true
        };
        db.MembershipPlans.Add(plan);
        await db.SaveChangesAsync();

        db.Memberships.Add(new Membership
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            BranchId = member.BranchId,
            StartDate = DateTime.UtcNow.Date.AddDays(-5),
            EndDate = DateTime.UtcNow.Date.AddDays(25),
            Status = MembershipStatus.Active,
            Source = MembershipSource.InGym,
            AutoRenewEnabled = false
        });
        await db.SaveChangesAsync();

        return (admin, member, session);
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
        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Admin",
            LastName = "User",
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(admin, "Admin");
        return admin;
    }

    private static async Task<Trainer> CreateTrainerAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Trainer;
        if (existing != null) return existing;
        var trainer = new Trainer
        {
            UserName = email,
            Email = email,
            FirstName = "Trainer",
            LastName = "User",
            Specialty = "S",
            Certification = "C",
            Experience = "E",
            IsActive = true,
            EmailConfirmed = true
        };
        var result = await userManager.CreateAsync(trainer, "Trainer@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(trainer, "Trainer");
        return trainer;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;
        var member = new Member
        {
            UserName = email,
            Email = email,
            FirstName = "Member",
            LastName = "User",
            MemberCode = $"M{Random.Shared.Next(1000, 9999)}",
            Gender = "M",
            Address = "A",
            EmergencyContact = "X",
            MedicalConditions = "None",
            IsActive = true,
            EmailConfirmed = true
        };
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
