using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Member> Members { get; set; }
        public DbSet<Trainer> Trainers { get; set; }
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<MembershipPlan> MembershipPlans { get; set; }
        public DbSet<WorkoutSession> WorkoutSessions { get; set; }
        public DbSet<MemberSession> MemberSessions { get; set; }
        public DbSet<TrainingPlan> TrainingPlans { get; set; }
        public DbSet<TrainingPlanItem> TrainingPlanItems { get; set; }
        public DbSet<NutritionPlan> NutritionPlans { get; set; }
        public DbSet<NutritionPlanItem> NutritionPlanItems { get; set; }
        public DbSet<TrainerMemberAssignment> TrainerMemberAssignments { get; set; }
        public DbSet<LoginAudit> LoginAudits { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
