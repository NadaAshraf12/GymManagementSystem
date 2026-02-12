using GymManagementSystem.Application.Interfaces;

namespace GymManagementSystem.WebUI.Services;

public class MembershipExpirationBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MembershipExpirationBackgroundService> _logger;

    public MembershipExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MembershipExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        await RunCycleAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested &&
               await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCycleAsync(stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var automationService = scope.ServiceProvider.GetRequiredService<ISubscriptionAutomationService>();

            var result = await automationService.ProcessExpirationsAsync(DateTime.UtcNow, stoppingToken);

            _logger.LogInformation(
                "Membership expiration cycle completed. Expired={ExpiredCount}, AutoRenewed={AutoRenewedCount}, AutoRenewSkipped={AutoRenewSkippedCount}",
                result.ExpiredCount,
                result.AutoRenewedCount,
                result.AutoRenewSkippedCount);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Membership expiration cycle failed.");
        }
    }
}

