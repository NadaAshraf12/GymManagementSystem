using GymManagementSystem.Application.Interfaces;
using GymManagementSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GymManagementSystem.Infrastructure.Data
{
    public class ApplicationDbContext :IdentityDbContext<ApplicationUser>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Identity DbSets (Inherited from IdentityDbContext)
        // public DbSet<ApplicationUser> Users { get; set; }

        // Derived User Types
        public DbSet<Member> Members { get; set; }
        public DbSet<Trainer> Trainers { get; set; }

        // Other DbSets
        public DbSet<Membership> Memberships { get; set; }
        public DbSet<MembershipPlan> MembershipPlans { get; set; }
        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<WorkoutSession> WorkoutSessions { get; set; }
        public DbSet<MemberSession> MemberSessions { get; set; }
        public DbSet<TrainingPlan> TrainingPlans { get; set; }
        public DbSet<TrainingPlanItem> TrainingPlanItems { get; set; }
        public DbSet<TrainerMemberAssignment> TrainerMemberAssignments { get; set; }
        public DbSet<LoginAudit> LoginAudits { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Decimal configurations
            builder.Entity<Trainer>(entity =>
            {
                entity.Property(t => t.Salary).HasColumnType("decimal(18,2)");
            });

            builder.Entity<MembershipPlan>(entity =>
            {
                entity.Property(mp => mp.Price).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Membership>(entity =>
            {
                entity.Property(m => m.PaidAmount).HasColumnType("decimal(18,2)");
            });

            builder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            });

            // Configure TPH Inheritance
            builder.Entity<ApplicationUser>()
                .HasDiscriminator<string>("UserType")
                .HasValue<ApplicationUser>("Base")
                .HasValue<Member>("Member")
                .HasValue<Trainer>("Trainer");

            // Member Configuration
            builder.Entity<Member>(entity =>
            {
                entity.HasIndex(m => m.MemberCode).IsUnique();
                entity.Property(m => m.MemberCode).IsRequired().HasMaxLength(20);
            });

            // MembershipPlan Configuration
            builder.Entity<MembershipPlan>(entity =>
            {
                entity.Property(mp => mp.Name).IsRequired().HasMaxLength(100);
                entity.Property(mp => mp.Price).HasColumnType("decimal(18,2)");
            });

            // Membership Configuration
            builder.Entity<Membership>(entity =>
            {
                entity.Property(m => m.PaidAmount).HasColumnType("decimal(18,2)");

                entity.HasOne(m => m.Member)
                    .WithMany(m => m.Memberships)
                    .HasForeignKey(m => m.MemberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(m => m.MembershipPlan)
                    .WithMany(mp => mp.Memberships)
                    .HasForeignKey(m => m.MembershipPlanId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Payment Configuration
            builder.Entity<Payment>(entity =>
            {
                entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");

                entity.HasOne(p => p.Membership)
                    .WithMany(m => m.Payments)
                    .HasForeignKey(p => p.MembershipId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // WorkoutSession Configuration
            builder.Entity<WorkoutSession>(entity =>
            {
                entity.HasOne(ws => ws.Trainer)
                    .WithMany(t => t.WorkoutSessions)
                    .HasForeignKey(ws => ws.TrainerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // MemberSession Configuration
            builder.Entity<MemberSession>(entity =>
            {
                entity.HasOne(ms => ms.Member)
                    .WithMany(m => m.MemberSessions)
                    .HasForeignKey(ms => ms.MemberId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(ms => ms.WorkoutSession)
                    .WithMany(ws => ws.MemberSessions)
                    .HasForeignKey(ms => ms.WorkoutSessionId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Attendance Configuration
            builder.Entity<Attendance>(entity =>
            {
                entity.HasOne(a => a.Member)
                    .WithMany(m => m.Attendances)
                    .HasForeignKey(a => a.MemberId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // TrainingPlan Configuration
            builder.Entity<TrainingPlan>(entity =>
            {
                entity.Property(tp => tp.Title).IsRequired().HasMaxLength(200);
                entity.HasOne(tp => tp.Member)
                    .WithMany()
                    .HasForeignKey(tp => tp.MemberId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(tp => tp.Trainer)
                    .WithMany()
                    .HasForeignKey(tp => tp.TrainerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TrainingPlanItem>(entity =>
            {
                entity.Property(i => i.ExerciseName).IsRequired().HasMaxLength(200);
                entity.HasOne(i => i.TrainingPlan)
                    .WithMany(tp => tp.Items)
                    .HasForeignKey(i => i.TrainingPlanId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // TrainerMemberAssignment Configuration
            builder.Entity<TrainerMemberAssignment>(entity =>
            {
                entity.HasIndex(x => new { x.TrainerId, x.MemberId }).IsUnique();
                entity.HasOne(a => a.Trainer)
                    .WithMany()
                    .HasForeignKey(a => a.TrainerId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(a => a.Member)
                    .WithMany()
                    .HasForeignKey(a => a.MemberId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // LoginAudit Configuration
            builder.Entity<LoginAudit>(entity =>
            {
                entity.HasOne(la => la.User)
                    .WithMany()
                    .HasForeignKey(la => la.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.Property(la => la.IpAddress).HasMaxLength(45);
                entity.Property(la => la.UserAgent).HasMaxLength(500);
                entity.Property(la => la.FailureReason).HasMaxLength(200);
            });

            // RefreshToken Configuration
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasOne(rt => rt.User)
                    .WithMany()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasIndex(rt => rt.Token).IsUnique();
                entity.Property(rt => rt.Token).IsRequired().HasMaxLength(500);
                entity.Property(rt => rt.IpAddress).HasMaxLength(45);
                entity.Property(rt => rt.UserAgent).HasMaxLength(500);
            });
        }
    }
}
