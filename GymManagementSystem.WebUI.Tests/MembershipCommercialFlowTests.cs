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

public class MembershipCommercialFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MembershipCommercialFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AdminCreatesMembership_WithCash_Active_NoWalletDebit_InvoiceAndCommissionCreated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, trainer, member) = await SeedUsersAsync(suffix);
        await SeedAssignmentAsync(trainer.Id, member.Id);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"Cash-{suffix}", 100);

        var create = await client.PostAsJsonAsync("/api/memberships/direct-create", new CreateDirectMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentMethod = PaymentMethod.Cash,
            PaymentAmount = 100
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var payload = await create.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(MembershipStatus.Active, payload!.Data!.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var id = payload.Data.Id;
        Assert.False(await db.WalletTransactions.AnyAsync(t => t.MemberId == member.Id && t.ReferenceId == id && t.Amount < 0));
        Assert.True(await db.Invoices.AnyAsync(i => i.MembershipId == id));
        Assert.True(await db.Commissions.AnyAsync(c => c.MembershipId == id && c.Source == CommissionSource.Activation));
    }

    [Fact]
    public async Task AdminCreatesMembership_WithWallet_Active_WalletDebited_InvoiceAndCommissionCreated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, trainer, member) = await SeedUsersAsync(suffix);
        await SeedAssignmentAsync(trainer.Id, member.Id);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"AdminWallet-{suffix}", 100);
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 150 });

        var create = await client.PostAsJsonAsync("/api/memberships/direct-create", new CreateDirectMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentMethod = PaymentMethod.Wallet
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);

        var payload = await create.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(MembershipStatus.Active, payload!.Data!.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var id = payload.Data.Id;
        var walletDebits = await db.WalletTransactions
            .Where(t => t.MemberId == member.Id && t.ReferenceId == id && t.Amount < 0)
            .ToListAsync();
        Assert.NotEmpty(walletDebits);
        Assert.Equal(-100, walletDebits.Sum(x => x.Amount));
        Assert.True(await db.Invoices.AnyAsync(i => i.MembershipId == id));
        Assert.True(await db.Commissions.AnyAsync(c => c.MembershipId == id && c.Source == CommissionSource.Activation));
    }

    [Fact]
    public async Task MemberSubscribes_WithWallet_Active_WalletDebited_InvoiceAndCommissionCreated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, trainer, member) = await SeedUsersAsync(suffix);
        await SeedAssignmentAsync(trainer.Id, member.Id);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"MemberWallet-{suffix}", 100);
        await client.PostAsJsonAsync("/api/wallet/adjust", new AdjustWalletDto { MemberId = member.Id, Amount = 180 });

        SetTestAuth(client, member.Id, "Member");
        var subscribe = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentMethod = PaymentMethod.Wallet
        });
        Assert.Equal(HttpStatusCode.Created, subscribe.StatusCode);

        var payload = await subscribe.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(MembershipStatus.Active, payload!.Data!.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var id = payload.Data.Id;
        Assert.True(await db.WalletTransactions.AnyAsync(t => t.MemberId == member.Id && t.ReferenceId == id && t.Amount == -100));
        Assert.True(await db.Invoices.AnyAsync(i => i.MembershipId == id));
        Assert.True(await db.Commissions.AnyAsync(c => c.MembershipId == id && c.Source == CommissionSource.Activation));
    }

    [Fact]
    public async Task MemberSubscribes_WithPaymentProof_Pending_NoWalletDebit_NoInvoice_NoCommission()
    {
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var (admin, trainer, member) = await SeedUsersAsync(suffix);
        await SeedAssignmentAsync(trainer.Id, member.Id);
        var client = _factory.CreateClient();

        SetTestAuth(client, admin.Id, "Admin");
        var plan = await CreatePlanAsync(client, $"Proof-{suffix}", 100);

        SetTestAuth(client, member.Id, "Member");
        var subscribe = await client.PostAsJsonAsync("/api/memberships/subscribe/online", new CreateMembershipDto
        {
            MemberId = member.Id,
            MembershipPlanId = plan.Id,
            PaymentMethod = PaymentMethod.Proof,
            PaymentAmount = 100
        });
        Assert.Equal(HttpStatusCode.Created, subscribe.StatusCode);

        var payload = await subscribe.Content.ReadFromJsonAsync<ApiResponse<MembershipReadDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.Equal(MembershipStatus.PendingPayment, payload!.Data!.Status);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var id = payload.Data.Id;
        Assert.False(await db.WalletTransactions.AnyAsync(t => t.MemberId == member.Id && t.ReferenceId == id && t.Amount < 0));
        Assert.False(await db.Invoices.AnyAsync(i => i.MembershipId == id));
        Assert.False(await db.Commissions.AnyAsync(c => c.MembershipId == id && c.Source == CommissionSource.Activation));
    }

    private async Task<MembershipPlanReadDto> CreatePlanAsync(HttpClient client, string name, decimal price)
    {
        var response = await client.PostAsJsonAsync("/api/membershipplans", new CreateMembershipPlanDto
        {
            Name = name,
            DurationInDays = 30,
            Price = price,
            IsActive = true
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiResponse<MembershipPlanReadDto>>(JsonOptions);
        return payload!.Data!;
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
                Notes = "commercial-flow"
            });
            await db.SaveChangesAsync();
        }
    }

    private async Task<(ApplicationUser Admin, Trainer Trainer, Member Member)> SeedUsersAsync(string suffix)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRoleAsync(roleManager, "Admin");
        await EnsureRoleAsync(roleManager, "Trainer");
        await EnsureRoleAsync(roleManager, "Member");

        var admin = await CreateAdminAsync(userManager, $"admin-commercial-{suffix}@test.local");
        var trainer = await CreateTrainerAsync(userManager, $"trainer-commercial-{suffix}@test.local");
        var member = await CreateMemberAsync(userManager, $"member-commercial-{suffix}@test.local");
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
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = "Admin", LastName = "Flow", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Admin@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Admin");
        return user;
    }

    private static async Task<Trainer> CreateTrainerAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Trainer;
        if (existing != null) return existing;
        var user = new Trainer { UserName = email, Email = email, FirstName = "Trainer", LastName = "Flow", Specialty = "General", Certification = "C", Experience = "E", IsActive = true, EmailConfirmed = true };
        var result = await userManager.CreateAsync(user, "Trainer@123");
        if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
        await userManager.AddToRoleAsync(user, "Trainer");
        return user;
    }

    private static async Task<Member> CreateMemberAsync(UserManager<ApplicationUser> userManager, string email)
    {
        var existing = await userManager.FindByEmailAsync(email) as Member;
        if (existing != null) return existing;
        var user = new Member { UserName = email, Email = email, FirstName = "Member", LastName = "Flow", MemberCode = $"M{Random.Shared.Next(1000, 9999)}", Gender = "M", Address = "A", EmergencyContact = "X", MedicalConditions = "None", IsActive = true, EmailConfirmed = true };
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
