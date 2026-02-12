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

public class CommissionFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public CommissionFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MembershipActivation_GeneratesCommission_AdminCanMarkPaid()
    {
        var (admin, trainer, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        await SeedAssignmentAsync(trainer.Id, member.Id);

        SetTestAuth(client, admin.Id, "Admin");
        var planResponse = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = "Commission Monthly",
            DurationInDays = 30,
            Price = 100,
            IsActive = true
        });
        var planPayload = await planResponse.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions);
        Assert.NotNull(planPayload);

        SetTestAuth(client, member.Id, "Member");
        var createMembership = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = planPayload!.Data!.Id,
            PaymentAmount = 100,
            WalletAmountToUse = 0
        });
        var membershipPayload = await createMembership.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(membershipPayload);
        var paymentId = membershipPayload!.Data!.Payments.Single().Id;

        SetTestAuth(client, admin.Id, "Admin");
        var confirm = await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/confirm", new ConfirmPaymentDto());
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        var unpaid = await client.GetAsync("/api/commissions/unpaid");
        Assert.Equal(HttpStatusCode.OK, unpaid.StatusCode);
        var unpaidPayload = await unpaid.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CommissionReadDto>>>(JsonOptions);
        Assert.NotNull(unpaidPayload);
        Assert.NotNull(unpaidPayload!.Data);
        var commission = Assert.Single(unpaidPayload.Data!);

        var markPaid = await client.PostAsync($"/api/commissions/{commission.Id}/mark-paid", null);
        Assert.Equal(HttpStatusCode.OK, markPaid.StatusCode);

        var metrics = await client.GetAsync("/api/commissions/metrics");
        Assert.Equal(HttpStatusCode.OK, metrics.StatusCode);
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
                Notes = "Commission test"
            });
            await db.SaveChangesAsync();
        }
    }

    private async Task<(ApplicationUser Admin, Trainer Trainer, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Trainer");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, "admin-commission@test.local");
        var trainer = await CreateTrainerAsync(userManager, "trainer-commission@test.local");
        var member = await CreateMemberAsync(userManager, "member-commission@test.local");
        return (admin, trainer, member);
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
        var user = new Member { UserName = email, Email = email, FirstName = "M", LastName = "M", MemberCode = "M9101", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
