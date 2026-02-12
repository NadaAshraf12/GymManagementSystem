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

public class WalletLedgerFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WalletLedgerFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task OverpaymentThenSessionDebit_CreatesLedgerAndComputedBalanceAndAudit()
    {
        var (admin, member, session) = await SeedUsersAndSessionAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var planCreate = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = "Monthly",
            DurationInDays = 30,
            Price = 100,
            IsActive = true
        });
        Assert.Equal(HttpStatusCode.Created, planCreate.StatusCode);

        var plan = await planCreate.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions);
        Assert.NotNull(plan);

        SetTestAuth(client, member.Id, "Member");
        var subscribe = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan!.Data!.Id,
            StartDate = DateTime.UtcNow.Date,
            PaymentAmount = 130,
            WalletAmountToUse = 0
        });
        Assert.Equal(HttpStatusCode.Created, subscribe.StatusCode);

        var subscribed = await subscribe.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(subscribed);
        var paymentId = subscribed!.Data!.Payments.Single().Id;

        SetTestAuth(client, admin.Id, "Admin");
        var confirm = await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/confirm", new ConfirmPaymentDto());
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        SetTestAuth(client, member.Id, "Member");
        var book = await client.PostAsJsonAsync("/api/sessions/book", new BookMemberToSessionDto
        {
            MemberId = member.Id,
            WorkoutSessionId = session.Id
        });
        Assert.Equal(HttpStatusCode.OK, book.StatusCode);

        var walletDebit = await client.PostAsJsonAsync("/api/wallet/use-for-session", new UseWalletForSessionBookingDto
        {
            MemberId = member.Id,
            WorkoutSessionId = session.Id,
            Amount = 10
        });
        Assert.Equal(HttpStatusCode.OK, walletDebit.StatusCode);

        var walletMe = await client.GetAsync("/api/wallet/me");
        Assert.Equal(HttpStatusCode.OK, walletMe.StatusCode);
        var balanceDto = await walletMe.Content.ReadFromJsonAsync<ApiResponse<WalletBalanceDto>>(JsonOptions);
        Assert.NotNull(balanceDto);
        Assert.Equal(20, balanceDto!.Data!.WalletBalance);

        var txResponse = await client.GetAsync($"/api/wallet/transactions/{member.Id}");
        Assert.Equal(HttpStatusCode.OK, txResponse.StatusCode);
        var txPayload = await txResponse.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<WalletTransactionReadDto>>>(JsonOptions);
        Assert.NotNull(txPayload);
        Assert.NotNull(txPayload!.Data);
        Assert.Contains(txPayload.Data!, t => t.Type == WalletTransactionType.Overpayment && t.Amount == 30);
        Assert.Contains(txPayload.Data!, t => t.Type == WalletTransactionType.SessionBooking && t.Amount == -10);

        await AssertWalletAuditAsync(member.Id);
    }

    private async Task AssertWalletAuditAsync(string memberId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var ledgerBalance = await db.WalletTransactions
            .Where(t => t.MemberId == memberId)
            .SumAsync(t => t.Amount);

        Assert.Equal(20, ledgerBalance);

        var auditLogs = await db.AuditLogs
            .Where(a => a.EntityName == nameof(WalletTransaction))
            .ToListAsync();

        Assert.NotEmpty(auditLogs);
    }

    private async Task<(ApplicationUser Admin, Member Member, WorkoutSession Session)> SeedUsersAndSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        await EnsureRoleAsync(roleManager, "Trainer");

        var admin = await CreateAdminAsync(userManager, "admin-wallet@test.local");
        var trainer = await CreateTrainerAsync(userManager, "trainer-wallet@test.local");
        var member = await CreateMemberAsync(userManager, "member-wallet@test.local");

        var session = await db.WorkoutSessions.FirstOrDefaultAsync(s => s.Title == "Wallet Session" && s.TrainerId == trainer.Id);
        if (session == null)
        {
            session = new WorkoutSession
            {
                TrainerId = trainer.Id,
                Title = "Wallet Session",
                Description = "Wallet booking test",
                SessionDate = DateTime.UtcNow.Date.AddDays(1),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                MaxParticipants = 20,
                CurrentParticipants = 0
            };

            db.WorkoutSessions.Add(session);
            await db.SaveChangesAsync();
        }

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

    private static async Task<Trainer> CreateTrainerAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Trainer;
        if (existing != null)
        {
            return existing;
        }

        var trainer = new Trainer
        {
            UserName = email,
            Email = email,
            FirstName = "Wallet",
            LastName = "Trainer",
            Specialty = "General",
            Certification = "CPT",
            Experience = "3 years",
            IsActive = true,
            HireDate = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(trainer, "Trainer@123");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(trainer, "Trainer");
        return trainer;
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
            FirstName = "Wallet",
            LastName = "Member",
            MemberCode = "M4001",
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
