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

public class InvoiceGenerationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public InvoiceGenerationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ConfirmPayment_CreatesInvoice_AndFileCanBeDownloaded()
    {
        var (admin, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var createPlan = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = "Invoice Monthly",
            DurationInDays = 30,
            Price = 100,
            IsActive = true
        });
        var planPayload = await createPlan.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions);

        SetTestAuth(client, member.Id, "Member");
        var subscribe = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = planPayload!.Data!.Id,
            PaymentAmount = 100
        });
        var subscribePayload = await subscribe.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        var paymentId = subscribePayload!.Data!.Payments.Single().Id;

        SetTestAuth(client, admin.Id, "Admin");
        var confirm = await client.PostAsJsonAsync($"/api/memberships/payments/{paymentId}/confirm", new ConfirmPaymentDto());
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        int invoiceId;
        string filePath;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var invoice = await db.Invoices.OrderByDescending(x => x.Id).FirstOrDefaultAsync(x => x.MemberId == member.Id);
            Assert.NotNull(invoice);
            Assert.False(string.IsNullOrWhiteSpace(invoice!.FilePath));
            Assert.True(File.Exists(invoice.FilePath));
            invoiceId = invoice.Id;
            filePath = invoice.FilePath;
        }

        var download = await client.GetAsync($"/api/invoices/{invoiceId}/download");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.True((await download.Content.ReadAsByteArrayAsync()).Length > 0);
        Assert.True(File.Exists(filePath));
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        var admin = await CreateAdminAsync(userManager, "admin-invoice@test.local");
        var member = await CreateMemberAsync(userManager, "member-invoice@test.local");
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
        var user = new Member { UserName = email, Email = email, FirstName = "M", LastName = "M", MemberCode = "M9201", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
