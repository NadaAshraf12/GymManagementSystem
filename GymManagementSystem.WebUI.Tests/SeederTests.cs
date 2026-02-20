using GymManagementSystem.Infrastructure.Data;
using GymManagementSystem.WebUI.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GymManagementSystem.WebUI.Tests;

public class SeederTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SeederTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Seeder_CreatesDefaultPlans_WhenDatabaseIsEmpty()
    {
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.MembershipPlans.RemoveRange(db.MembershipPlans);
            await db.SaveChangesAsync();
        }

        await DbSeeder.SeedAsync(_factory.Services);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var names = await verifyDb.MembershipPlans
            .AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted)
            .Select(x => x.Name)
            .ToListAsync();

        Assert.Contains("Monthly Basic", names);
        Assert.Contains("Monthly Premium", names);
        Assert.Contains("Yearly Premium", names);
    }
}
