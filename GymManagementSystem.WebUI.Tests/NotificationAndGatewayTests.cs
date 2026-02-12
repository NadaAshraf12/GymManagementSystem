using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class NotificationAndGatewayTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public NotificationAndGatewayTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PaymentRejected_CreatesMemberNotification_AndGatewaySwitchKeepsFlowWorking()
    {
        var (admin, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        Environment.SetEnvironmentVariable("PAYMENT_GATEWAY", "future-online");
        try
        {
            SetTestAuth(client, admin.Id, "Admin");
            var planResponse = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
            {
                Name = "Notif Monthly",
                DurationInDays = 30,
                Price = 100,
                IsActive = true
            });
            var planPayload = await planResponse.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions);

            SetTestAuth(client, member.Id, "Member");
            var subscribe = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
            {
                MemberId = member.Id,
                MembershipPlanId = planPayload!.Data!.Id,
                PaymentAmount = 100
            });
            Assert.Equal(HttpStatusCode.Created, subscribe.StatusCode);

            var subscribePayload = await subscribe.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
            Assert.NotNull(subscribePayload);
            Assert.Equal(1, subscribePayload!.Data!.Payments.Count);
            var paymentId = subscribePayload.Data.Payments.Single().Id;

            SetTestAuth(client, admin.Id, "Admin");
            var reject = await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/review", new ReviewPaymentDto
            {
                Approve = false,
                RejectionReason = "Invalid receipt"
            });
            Assert.Equal(HttpStatusCode.OK, reject.StatusCode);

            SetTestAuth(client, member.Id, "Member");
            var notifications = await client.GetAsync("/api/notifications/me");
            Assert.Equal(HttpStatusCode.OK, notifications.StatusCode);
            var notificationPayload = await notifications.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<NotificationReadDto>>>(JsonOptions);
            Assert.NotNull(notificationPayload);
            Assert.NotNull(notificationPayload!.Data);
            Assert.Contains(notificationPayload.Data!, n => n.Title.Contains("Payment Rejected"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("PAYMENT_GATEWAY", null);
        }
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        var admin = await CreateAdminAsync(userManager, "admin-notif@test.local");
        var member = await CreateMemberAsync(userManager, "member-notif@test.local");
        return (admin, member);
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

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;
        var user = new Member { UserName = email, Email = email, FirstName = "M", LastName = "M", MemberCode = "M9301", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
