using Microsoft.EntityFrameworkCore;
using Sentra.API.Models;

namespace Sentra.API.Data
{
    public class SentraDbContext : DbContext
    {
        public SentraDbContext(DbContextOptions<SentraDbContext> options)
            : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Camera> Cameras { get; set; }
        public DbSet<Incident> Incidents { get; set; }
        public DbSet<Alert> Alerts { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ===== USER =====
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.Property(u => u.Role).HasDefaultValue(UserRoles.Resident);
                entity.Property(u => u.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            // ===== CAMERA =====
            modelBuilder.Entity<Camera>(entity =>
            {
                entity.HasOne(c => c.User)
                      .WithMany(u => u.Cameras)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(c => c.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Soft delete filter — hides deleted cameras automatically
                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            // ===== INCIDENT =====
            modelBuilder.Entity<Incident>(entity =>
            {
                entity.HasOne(i => i.Camera)
                      .WithMany(c => c.Incidents)
                      .HasForeignKey(i => i.CameraId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(i => i.ResolvedByUser)
                      .WithMany()
                      .HasForeignKey(i => i.ResolvedByUserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.Property(i => i.Timestamp)
                      .HasDefaultValueSql("GETUTCDATE()");

                // Indexes for dashboard queries
                entity.HasIndex(i => i.Timestamp);
                entity.HasIndex(i => i.Type);
                entity.HasIndex(i => i.Status);
            });

            // ===== ALERT =====
            modelBuilder.Entity<Alert>(entity =>
            {
                entity.HasOne(a => a.Incident)
                      .WithMany(i => i.Alerts)
                      .HasForeignKey(a => a.IncidentId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(a => a.User)
                      .WithMany()
                      .HasForeignKey(a => a.UserId)
                      .OnDelete(DeleteBehavior.NoAction);

                entity.Property(a => a.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
            });

            // ===== REFRESH TOKEN =====
            modelBuilder.Entity<RefreshToken>(entity =>
            {
                entity.HasOne(r => r.User)
                      .WithMany()
                      .HasForeignKey(r => r.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.Property(r => r.CreatedAt)
                      .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(r => r.Token).IsUnique();
            });
        }
    }
}