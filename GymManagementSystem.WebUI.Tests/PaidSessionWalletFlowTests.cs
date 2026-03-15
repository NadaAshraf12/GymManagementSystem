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

public class PaidSessionWalletFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaidSessionWalletFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MemberWithWallet_BooksPaidSession_WalletDebited_BookingAndAuditCreated()
    {
        var (admin, member, session) = await SeedUsersAndPaidSessionAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var adjust = await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto
        {
            MemberId = member.Id,
            Amount = 200
        });
        Assert.Equal(HttpStatusCode.OK, adjust.StatusCode);

        SetTestAuth(client, member.Id, "Member");
        var book = await client.PostAsJsonAsync("/api/sessions/book-paid", new PaidSessionBookingDto
        {
            MemberId = member.Id,
            WorkoutSessionId = session.Id
        });
        Assert.Equal(HttpStatusCode.OK, book.StatusCode);
        var payload = await book.Content.ReadFromJsonAsync<ApiResponse<SessionBookingResultDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.True(payload.Data!.Success);
        Assert.Equal(150, payload.Data.WalletBalance);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.MemberSessions.AnyAsync(ms => ms.MemberId == member.Id && ms.WorkoutSessionId == session.Id));
        Assert.True(await db.WalletTransactions.AnyAsync(t =>
            t.MemberId == member.Id &&
            t.Type == WalletTransactionType.SessionBooking &&
            t.ReferenceId == session.Id &&
            t.Amount == -50));
        Assert.True(await db.AuditLogs.AnyAsync(a => a.EntityName == nameof(WalletTransaction)));
    }

    private async Task<(ApplicationUser Admin, Member Member, WorkoutSession Session)> SeedUsersAndPaidSessionAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        await EnsureRoleAsync(roleManager, "Trainer");

        var admin = await CreateAdminAsync(userManager, "admin-paid-session@test.local");
        var trainer = await CreateTrainerAsync(userManager, "trainer-paid-session@test.local");
        var member = await CreateMemberAsync(userManager, "member-paid-session@test.local");

        var session = await db.WorkoutSessions.FirstOrDefaultAsync(s => s.Title == "Paid Session" && s.TrainerId == trainer.Id);
        if (session == null)
        {
            session = new WorkoutSession
            {
                TrainerId = trainer.Id,
                BranchId = member.BranchId,
                Title = "Paid Session",
                Description = "Paid session",
                SessionDate = DateTime.UtcNow.Date.AddDays(1),
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(10, 0, 0),
                MaxParticipants = 20,
                CurrentParticipants = 0,
                Price = 50
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
        if (existing != null) return existing;
        var admin = new ApplicationUser { UserName = email, Email = email, FirstName = "Admin", LastName = "User", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(admin, "Admin");
        return admin;
    }

    private static async Task<Trainer> CreateTrainerAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Trainer;
        if (existing != null) return existing;
        var trainer = new Trainer { UserName = email, Email = email, FirstName = "Trainer", LastName = "User", Specialty = "S", Certification = "C", Experience = "E", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(trainer, "Trainer@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(trainer, "Trainer");
        return trainer;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;
        var member = new Member { UserName = email, Email = email, FirstName = "Member", LastName = "User", MemberCode = "M9501", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
