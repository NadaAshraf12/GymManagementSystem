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
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Commission> Commissions { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AddOn> AddOns { get; set; }
        public DbSet<WalletTransaction> WalletTransactions { get; set; }
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

            builder.Entity<ApplicationUser>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<Membership>().HasQueryFilter(x => !x.IsDeleted);
            builder.Entity<MembershipPlan>().HasQueryFilter(x => !x.IsDeleted);
        }
    }
}
