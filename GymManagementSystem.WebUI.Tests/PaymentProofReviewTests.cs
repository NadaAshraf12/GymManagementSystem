using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GymManagementSystem.Application.DTOs;
using GymManagementSystem.Domain.Entities;
using GymManagementSystem.Domain.Enums;
using GymManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class PaymentProofReviewTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public PaymentProofReviewTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MemberUploadsProof_AdminRejects_MembershipRemainsNotActive()
    {
        var (admin, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var planCreate = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = "Monthly Proof",
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
            PaymentAmount = 100,
            WalletAmountToUse = 0
        });
        Assert.Equal(HttpStatusCode.Created, subscribe.StatusCode);

        var subscribed = await subscribe.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(subscribed);

        var paymentId = subscribed!.Data!.Payments.Single().Id;
        var membershipId = subscribed.Data.Id;

        using var multipart = new MultipartFormDataContent();
        var bytes = Encoding.UTF8.GetBytes("fake-image-content");
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        multipart.Add(fileContent, "file", "receipt.jpg");

        var upload = await client.PostAsync($"/api/memberships/payments/{paymentId}/proof", multipart);
        Assert.Equal(HttpStatusCode.OK, upload.StatusCode);

        SetTestAuth(client, admin.Id, "Admin");
        var pending = await client.GetAsync("/api/memberships/payments/pending");
        Assert.Equal(HttpStatusCode.OK, pending.StatusCode);

        var pendingPayload = await pending.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<PendingPaymentReadDto>>>(JsonOptions);
        Assert.NotNull(pendingPayload);
        Assert.Contains(pendingPayload!.Data!, p => p.PaymentId == paymentId && !string.IsNullOrWhiteSpace(p.PaymentProofUrl));

        var review = await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/review", new ReviewPaymentDto
        {
            Approve = false,
            RejectionReason = "Receipt is unclear"
        });
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);

        var reviewed = await review.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(reviewed);
        Assert.True(reviewed!.Success);
        Assert.NotEqual(MembershipStatus.Active, reviewed.Data!.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId);
        Assert.NotNull(membership);
        Assert.NotEqual(MembershipStatus.Active, membership!.Status);

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
        Assert.NotNull(payment);
        Assert.Equal(PaymentStatus.Rejected, payment!.PaymentStatus);
        Assert.Equal("Receipt is unclear", payment.RejectionReason);

        var paymentAuditExists = await db.AuditLogs
            .AnyAsync(a => a.EntityName == nameof(Payment) && a.EntityId == paymentId.ToString() && a.Action == "Modified");
        Assert.True(paymentAuditExists);
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, "admin-proof@test.local");
        var member = await CreateMemberAsync(userManager, "member-proof@test.local");

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
        if (existing != null)
        {
            return existing;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Proof",
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
            FirstName = "Proof",
            LastName = "Member",
            MemberCode = "M6001",
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
