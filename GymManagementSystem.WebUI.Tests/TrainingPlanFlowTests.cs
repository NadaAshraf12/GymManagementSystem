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

public class TrainingPlanFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public TrainingPlanFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task TrainerFlow_CreatesUpdatesReadsAndAudits()
    {
        var (trainer1, trainer2, member) = await SeedUsersAsync();

        var client = _factory.CreateClient();
        SetTestAuth(client, trainer1.Id, "Trainer");

        var createDto = new CreateTrainingPlanDto
        {
            MemberId = member.Id,
            TrainerId = trainer1.Id,
            Title = "Strength Block A",
            Notes = "Initial plan"
        };

        var createResponse = await client.PostAsJsonAsync("/api/plans/training", createDto);
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var body = await createResponse.Content.ReadAsStringAsync();
            throw new Xunit.Sdk.XunitException($"Create failed: {createResponse.StatusCode}. Body: {body}");
        }

        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<int>>(JsonOptions);
        Assert.NotNull(created);
        Assert.True(created!.Success);
        Assert.True(created.Data > 0);

        var planId = created.Data;

        var updateDto = new UpdateTrainingPlanDto
        {
            Id = planId,
            Title = "Strength Block B",
            Notes = "Revised plan"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/plans/training/{planId}", updateDto);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ApiResponse<TrainingPlanDto>>(JsonOptions);
        Assert.NotNull(updated);
        Assert.True(updated!.Success);
        Assert.Equal("Strength Block B", updated.Data?.Title);

        var getResponse = await client.GetAsync($"/api/plans/training/{planId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ApiResponse<TrainingPlanDto>>(JsonOptions);
        Assert.NotNull(fetched);
        Assert.True(fetched!.Success);
        Assert.Equal(planId, fetched.Data?.Id);

        await AssertAuditLogAsync(planId, trainer1.Id, "Strength Block A", "Strength Block B");

        SetTestAuth(client, trainer2.Id, "Trainer");
        var unauthorizedResponse = await client.PutAsJsonAsync($"/api/plans/training/{planId}", updateDto);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
    }

    private async Task<(Trainer Trainer1, Trainer Trainer2, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(roleManager, "Trainer");
        await EnsureRoleAsync(roleManager, "Member");

        var trainer1 = await CreateTrainerAsync(userManager, "trainer1@test.local");
        var trainer2 = await CreateTrainerAsync(userManager, "trainer2@test.local");
        var member = await CreateMemberAsync(userManager, "member1@test.local");

        return (trainer1, trainer2, member);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string role)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
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
            FirstName = "Test",
            LastName = "Trainer",
            Specialty = "Strength",
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
            FirstName = "Test",
            LastName = "Member",
            MemberCode = "M1001",
            Gender = "M",
            Address = "",
            EmergencyContact = "",
            MedicalConditions = "",
            IsActive = true,
            JoinDate = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(member, "Member@123");
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(member, "Member");
        return member;
    }

    private async Task AssertAuditLogAsync(int planId, string trainerId, string oldTitle, string newTitle)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var logs = await db.AuditLogs
            .Where(a => a.EntityName == nameof(TrainingPlan) && a.EntityId == planId.ToString())
            .ToListAsync();

        Assert.NotEmpty(logs);

        var modified = logs.SingleOrDefault(a => a.Action == "Modified");
        Assert.NotNull(modified);
        Assert.Equal(trainerId, modified!.UserId);

        using var oldDoc = JsonDocument.Parse(modified.OldValues ?? "{}");
        using var newDoc = JsonDocument.Parse(modified.NewValues ?? "{}");

        Assert.True(oldDoc.RootElement.TryGetProperty("Title", out var oldTitleElement));
        Assert.True(newDoc.RootElement.TryGetProperty("Title", out var newTitleElement));

        Assert.Equal(oldTitle, oldTitleElement.GetString());
        Assert.Equal(newTitle, newTitleElement.GetString());
    }

    private static void SetTestAuth(HttpClient client, string userId, string roles)
    {
        client.DefaultRequestHeaders.Remove(TestAuthHandler.UserIdHeader);
        client.DefaultRequestHeaders.Remove(TestAuthHandler.RolesHeader);
        client.DefaultRequestHeaders.Add(TestAuthHandler.UserIdHeader, userId);
        client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);
    }
}
