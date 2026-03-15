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

public class AddOnPurchaseFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public AddOnPurchaseFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MemberPurchasesAddOn_UsesWallet_CreatesInvoiceAndNotification()
    {
        var (admin, member) = await SeedUsersAsync();
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto
        {
            MemberId = member.Id,
            Amount = 120
        });

        var createAddOn = await client.PostAsJsonAsync("/api/addons", new CreateAddOnDto
        {
            Name = "Protein Pack",
            Price = 40
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
        Assert.Equal(HttpStatusCode.OK, purchase.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.True(await db.WalletTransactions.AnyAsync(t =>
            t.MemberId == member.Id &&
            t.Type == WalletTransactionType.AddOnPurchase &&
            t.Amount == -40));
        Assert.True(await db.Invoices.AnyAsync(i => i.MemberId == member.Id && i.AddOnId == addOnPayload.Data.Id));
        Assert.True(await db.Notifications.AnyAsync(n => n.UserId == member.Id && n.Title.Contains("Add-On")));
    }

    private async Task<(ApplicationUser Admin, Member Member)> SeedUsersAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Member");
        var admin = await CreateAdminAsync(userManager, "admin-addon@test.local");
        var member = await CreateMemberAsync(userManager, "member-addon@test.local");
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
        var admin = new ApplicationUser { UserName = email, Email = email, FirstName = "Admin", LastName = "User", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(admin, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(admin, "Admin");
        return admin;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;
        var member = new Member { UserName = email, Email = email, FirstName = "Member", LastName = "User", MemberCode = "M9701", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
