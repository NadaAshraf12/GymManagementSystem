using GymManagementSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        // Identity DbSets
        DbSet<ApplicationUser> Users { get; }
        DbSet<Member> Members { get; }
        DbSet<Trainer> Trainers { get; }

        // Other DbSets
        DbSet<Membership> Memberships { get; }
        DbSet<MembershipPlan> MembershipPlans { get; }
        DbSet<Attendance> Attendances { get; }
        DbSet<Payment> Payments { get; }
        DbSet<WorkoutSession> WorkoutSessions { get; }
        DbSet<MemberSession> MemberSessions { get; }
        DbSet<TrainingPlan> TrainingPlans { get; }
        DbSet<TrainingPlanItem> TrainingPlanItems { get; }
        DbSet<NutritionPlan> NutritionPlans { get; }
        DbSet<NutritionPlanItem> NutritionPlanItems { get; }
        DbSet<TrainerMemberAssignment> TrainerMemberAssignments { get; }
        DbSet<LoginAudit> LoginAudits { get; }
        DbSet<RefreshToken> RefreshTokens { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
