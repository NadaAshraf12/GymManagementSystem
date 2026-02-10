using System.Text.Json;
using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GymManagementSystem.Infrastructure.Data;

public class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly HashSet<Type> AuditedEntities = new()
    {
        typeof(TrainingPlan),
        typeof(TrainingPlanItem),
        typeof(NutritionPlan),
        typeof(NutritionPlanItem),
        typeof(WorkoutSession),
        typeof(TrainerMemberAssignment)
    };

    private readonly ICurrentUserService _currentUserService;

    public AuditSaveChangesInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context == null)
        {
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        var auditEntries = CreateAuditEntries(context.ChangeTracker);
        if (auditEntries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private List<AuditLog> CreateAuditEntries(ChangeTracker changeTracker)
    {
        var result = new List<AuditLog>();
        var userId = _currentUserService.UserId;

        foreach (var entry in changeTracker.Entries())
        {
            if (entry.Entity == null || entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
            {
                continue;
            }

            if (!AuditedEntities.Contains(entry.Entity.GetType()))
            {
                continue;
            }

            var audit = new AuditLog
            {
                EntityName = entry.Entity.GetType().Name,
                EntityId = GetPrimaryKey(entry),
                Action = entry.State.ToString(),
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            var (oldValues, newValues) = GetChanges(entry);
            audit.OldValues = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null;
            audit.NewValues = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;

            result.Add(audit);
        }

        return result;
    }

    private static (Dictionary<string, object?> OldValues, Dictionary<string, object?> NewValues) GetChanges(EntityEntry entry)
    {
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();

        foreach (var prop in entry.Properties)
        {
            if (prop.Metadata.IsPrimaryKey())
            {
                continue;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    newValues[prop.Metadata.Name] = prop.CurrentValue;
                    break;
                case EntityState.Deleted:
                    oldValues[prop.Metadata.Name] = prop.OriginalValue;
                    break;
                case EntityState.Modified:
                    if (prop.IsModified)
                    {
                        oldValues[prop.Metadata.Name] = prop.OriginalValue;
                        newValues[prop.Metadata.Name] = prop.CurrentValue;
                    }
                    break;
            }
        }

        return (oldValues, newValues);
    }

    private static string GetPrimaryKey(EntityEntry entry)
    {
        var key = entry.Metadata.FindPrimaryKey();
        if (key == null)
        {
            return string.Empty;
        }

        var parts = key.Properties
            .Select(p => entry.Property(p.Name).CurrentValue?.ToString())
            .Where(v => !string.IsNullOrWhiteSpace(v));

        return string.Join("|", parts);
    }
}