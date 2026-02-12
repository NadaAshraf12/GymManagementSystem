using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class WalletConcurrencyTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public WalletConcurrencyTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConcurrentWalletDebit_DoesNotCreateNegativeBalance_AndFailsGracefully()
    {
        var (admin, member, sessionId) = await SeedUsersAndSessionAsync();
        var adminClient = _factory.CreateClient();
        SetTestAuth(adminClient, admin.Id, "Admin");

        var adjust = await adminClient.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto
        {
            MemberId = member.Id,
            Amount = 10
        });
        Assert.Equal(HttpStatusCode.OK, adjust.StatusCode);

        var memberClient1 = _factory.CreateClient();
        var memberClient2 = _factory.CreateClient();
        SetTestAuth(memberClient1, member.Id, "Member");
        SetTestAuth(memberClient2, member.Id, "Member");

        var request = new UseWalletForSessionBookingDto
        {
            MemberId = member.Id,
            WorkoutSessionId = sessionId,
            Amount = 10
        };

        var task1 = memberClient1.PostAsJsonAsync("/api/wallet/use-for-session", request);
        var task2 = memberClient2.PostAsJsonAsync("/api/wallet/use-for-session", request);

        var responses = await Task.WhenAll(task1, task2);
        Assert.All(responses, r =>
        {
            Assert.Contains(r.StatusCode, new[]
            {
                HttpStatusCode.OK,
                HttpStatusCode.BadRequest,
                HttpStatusCode.Conflict
            });
        });

        var wallet = await memberClient1.GetAsync("/api/wallet/me");
        Assert.Equal(HttpStatusCode.OK, wallet.StatusCode);
        var payload = await wallet.Content.ReadFromJsonAsync<ApiResponse<WalletBalanceDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Data!.WalletBalance >= 0);
        Assert.True(payload.Data.WalletBalance <= 10);
    }

    private async Task<(ApplicationUser Admin, Member Member, int SessionId)> SeedUsersAndSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        await EnsureRoleAsync(roleManager, "Trainer");

        var key = Guid.NewGuid().ToString("N");
        var admin = await CreateAdminAsync(userManager, $"admin-concurrency-{key}@test.local");
        var trainer = await CreateTrainerAsync(userManager, $"trainer-concurrency-{key}@test.local");
        var member = await CreateMemberAsync(userManager, $"member-concurrency-{key}@test.local");

        var session = await db.WorkoutSessions.FirstOrDefaultAsync(s => s.Title == "Concurrency Session" && s.TrainerId == trainer.Id);
        if (session == null)
        {
            session = new WorkoutSession
            {
                TrainerId = trainer.Id,
                Title = "Concurrency Session",
                Description = "Wallet concurrency test",
                SessionDate = DateTime.UtcNow.Date.AddDays(1),
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                MaxParticipants = 15,
                CurrentParticipants = 0
            };
            db.WorkoutSessions.Add(session);
            await db.SaveChangesAsync();
        }

        return (admin, member, session.Id);
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
            FirstName = "Concurrency",
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
            FirstName = "Concurrency",
            LastName = "Trainer",
            Specialty = "General",
            Certification = "CPT",
            Experience = "2 years",
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
            FirstName = "Concurrency",
            LastName = "Member",
            MemberCode = "M8001",
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
